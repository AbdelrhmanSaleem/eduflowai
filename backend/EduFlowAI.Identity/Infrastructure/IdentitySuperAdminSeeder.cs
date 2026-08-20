using EduFlowAI.Identity.Domain.Constants;
using EduFlowAI.Identity.Domain.Entities;
using EduFlowAI.Identity.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduFlowAI.Identity.Infrastructure;

internal sealed class IdentitySuperAdminSeeder(
    IServiceProvider serviceProvider,
    IOptions<BootstrapSuperAdminOptions> options,
    ILogger<IdentitySuperAdminSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bootstrapOptions = options.Value;

        if (!bootstrapOptions.Enabled)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var email = bootstrapOptions.Email
            .Trim()
            .ToLowerInvariant();

        await using var scope = serviceProvider.CreateAsyncScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<AppUser>>();

        var user = await userManager.FindByEmailAsync(email);
        var wasCreated = false;

        if (user is null)
        {
            var now = DateTimeOffset.UtcNow;

            user = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsActive = true,
                PreferredLanguage = "en",
                GmailNotificationsEnabled = false,
                LockoutEnabled = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var createResult = await userManager.CreateAsync(
                user,
                bootstrapOptions.Password);

            EnsureSucceeded(
                createResult,
                $"create bootstrap Super Admin '{email}'");

            wasCreated = true;
        }
        else
        {
            if (!user.IsActive)
            {
                throw new InvalidOperationException(
                    $"The bootstrap Super Admin account '{email}' exists " +
                    "but is inactive. It will not be reactivated " +
                    "automatically.");
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                user.UpdatedAt = DateTimeOffset.UtcNow;

                var updateResult = await userManager.UpdateAsync(user);

                EnsureSucceeded(
                    updateResult,
                    $"confirm bootstrap Super Admin email '{email}'");
            }
        }

        if (!await userManager.IsInRoleAsync(
                user,
                AppRoles.SuperAdmin))
        {
            var roleResult = await userManager.AddToRoleAsync(
                user,
                AppRoles.SuperAdmin);

            EnsureSucceeded(
                roleResult,
                $"assign the SuperAdmin role to '{email}'");
        }

        if (wasCreated)
        {
            logger.LogWarning(
                "Bootstrap Super Admin {Email} was created. Disable " +
                "BootstrapSuperAdmin and remove its password from " +
                "configuration after verifying login.",
                email);
        }
        else
        {
            logger.LogInformation(
                "Bootstrap Super Admin {Email} already exists and has " +
                "the required role.",
                email);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    private static void EnsureSucceeded(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error =>
                $"{error.Code}: {error.Description}"));

        throw new InvalidOperationException(
            $"Could not {operation}: {errors}");
    }
}
