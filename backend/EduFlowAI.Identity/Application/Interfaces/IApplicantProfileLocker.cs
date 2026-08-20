namespace EduFlowAI.Identity.Application.Interfaces;

public interface IApplicantProfileLocker
{
    Task LockAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
