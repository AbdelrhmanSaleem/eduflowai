using EduFlowAI.Identity.Domain.Entities;

namespace EduFlowAI.Identity.Application.Interfaces;

public interface IIdentityEmailSender
{
    Task SendConfirmationTokenAsync(
        AppUser user,
        string token,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetTokenAsync(
        AppUser user,
        string token,
        CancellationToken cancellationToken = default);
}