namespace EduFlowAI.Admission.Application.Interfaces.Services
{
    public interface IAdminOperationsService
    {
        Task<(bool IsSuccess, string? ErrorMessage)> OverrideEligibilityAsync(Guid applicationId, string adminId, string overrideReason);
    }
}
