using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Identity.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EduFlowAI.Identity.Infrastructure;

internal sealed class DevelopmentIdentityEmailSender(
    ILogger<DevelopmentIdentityEmailSender> logger,
    IHostEnvironment environment) : IIdentityEmailSender
{
    public Task SendConfirmationTokenAsync(
        AppUser user,
        string token,
        CancellationToken cancellationToken = default)
    {
        LogToken("email-confirmation", user, token);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetTokenAsync(
        AppUser user,
        string token,
        CancellationToken cancellationToken = default)
    {
        LogToken("password-reset", user, token);
        return Task.CompletedTask;
    }

    private void LogToken(string purpose, AppUser user, string token)
    {
        if (environment.IsDevelopment())
        {
            logger.LogInformation(
                "Development {Purpose} token for {Email}: {Token}",
                purpose,
                user.Email,
                token);
            return;
        }

        logger.LogWarning(
            "No production Identity email sender is configured for " +
            "{Purpose} messages to {Email}.",
            purpose,
            user.Email);
    }
}