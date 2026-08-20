using EduFlowAI.Identity.Domain.Constants;
using EduFlowAI.Identity.Domain.Entities;
using EduFlowAI.Identity.Infrastructure;
using EduFlowAI.Identity.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EduFlowAI.Identity.Tests;

public sealed class IdentitySuperAdminSeederTests
{
    [Fact]
    public async Task StartAsync_WhenDisabled_DoesNotCreateUser()
    {
        await using var provider = CreateServiceProvider();
        var seeder = CreateSeeder(
            provider,
            new BootstrapSuperAdminOptions
            {
                Enabled = false
            });

        await seeder.StartAsync(CancellationToken.None);

        await using var scope = provider.CreateAsyncScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<AppUser>>();

        Assert.Empty(userManager.Users);
    }

    [Fact]
    public async Task StartAsync_WhenEnabled_IsIdempotentAndDoesNotResetPassword()
    {
        const string email = "admin@eduflowai.test";
        const string originalPassword = "InitialAdmin123";
        const string replacementPassword = "ReplacementAdmin456";

        await using var provider = CreateServiceProvider();
        await CreateSuperAdminRoleAsync(provider);

        var firstSeeder = CreateSeeder(
            provider,
            new BootstrapSuperAdminOptions
            {
                Enabled = true,
                Email = email,
                Password = originalPassword
            });

        await firstSeeder.StartAsync(CancellationToken.None);

        var secondSeeder = CreateSeeder(
            provider,
            new BootstrapSuperAdminOptions
            {
                Enabled = true,
                Email = email,
                Password = replacementPassword
            });

        await secondSeeder.StartAsync(CancellationToken.None);

        await using var scope = provider.CreateAsyncScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<AppUser>>();

        var users = await userManager.Users.ToListAsync();
        var user = Assert.Single(users);

        Assert.Equal(email, user.Email);
        Assert.True(user.EmailConfirmed);
        Assert.True(user.IsActive);
        Assert.True(await userManager.IsInRoleAsync(
            user,
            AppRoles.SuperAdmin));
        Assert.True(await userManager.CheckPasswordAsync(
            user,
            originalPassword));
        Assert.False(await userManager.CheckPasswordAsync(
            user,
            replacementPassword));
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();

        services.AddLogging();
        services.AddDbContext<SeederIdentityDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<SeederIdentityDbContext>();

        return services.BuildServiceProvider();
    }

    private static IdentitySuperAdminSeeder CreateSeeder(
        IServiceProvider serviceProvider,
        BootstrapSuperAdminOptions options) =>
        new(
            serviceProvider,
            Options.Create(options),
            NullLogger<IdentitySuperAdminSeeder>.Instance);

    private static async Task CreateSuperAdminRoleAsync(
        IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var result = await roleManager.CreateAsync(
            new IdentityRole(AppRoles.SuperAdmin));

        Assert.True(
            result.Succeeded,
            string.Join(
                "; ",
                result.Errors.Select(error => error.Description)));
    }

    private sealed class SeederIdentityDbContext(
        DbContextOptions<SeederIdentityDbContext> options)
        : IdentityDbContext<AppUser>(options);
}
