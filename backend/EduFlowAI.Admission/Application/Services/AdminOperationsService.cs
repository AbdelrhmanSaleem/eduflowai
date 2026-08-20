using EduFlowAI.Admission.Application.Interfaces.Repositories;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Admission.Application.Services
{
    public class AdminOperationsService : IAdminOperationsService
    {
        private readonly IApplicationRepository _applicationRepository;

        public AdminOperationsService(IApplicationRepository applicationRepository)
        {
            _applicationRepository = applicationRepository;
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> OverrideEligibilityAsync(Guid applicationId, string adminId, string overrideReason)
        {
            // 1. Fetch the application
            var application = await _applicationRepository.GetFirstOrDefaultAsync(app => app.Id == applicationId);
            if(application == null)
            {
                return (false, "Application not found");
            }

            // 2. Validate current state: The application MUST be in a rejected state to be overridden
            if(application.Status != ApplicationStatus.EligibilityFailed && application.Status != Domain.Enums.ApplicationStatus.DocumentRejected)
            {
                return (false, $"Cannot override eligibility for an application in '{application.Status}' status. It must be rejected.");
            }

            // 3. Update the status to bypass eligibility and enter the assessment phase
            application.Status = ApplicationStatus.AssessmentInProgress;
            application.UpdatedAt = DateTimeOffset.UtcNow;
            application.AdminNotes = overrideReason;
            // 4. Save changes
            _applicationRepository.Update(application);
            await _applicationRepository.SaveChangesAsync();

            return (true, null);
        }
    }
}
