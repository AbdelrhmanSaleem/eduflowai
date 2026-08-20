namespace EduFlowAI.Admission.Application.Interfaces.Services
{
    public interface IApplicationAcessReader
    {
        Task<bool> CanAccessDocumentsAsync(Guid applicationId, string userId, CancellationToken cancellationToken);

        Task<Guid> GetProgramIdAsync(Guid applicationId, CancellationToken cancellationToken);

        // Used by AI's document verification context reader to resolve the applicant profile
        // for an application without AI needing to query the Application table directly.
        Task<string> GetApplicantUserIdAsync(Guid applicationId, CancellationToken cancellationToken);
        Task<Guid> GetApplicationIdForUserAsync(string userId, CancellationToken cancellationToken);
    }
}
