using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Shared.Kernel.Common.Pagination;

namespace EduFlowAI.Admission.Application.Interfaces.Services
{
    public interface IApplicationStatusQueryService
    {
        /// <summary>
        /// Retrieves the current status of an application by its ID
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task<ApplicationStatusDto?> GetApplicationStatusAsync(Guid applicationId);

        /// <summary>
        /// Retrieves the current status of an application for a given applicant user ID.
        /// </summary>
        /// <param name="applicantUserId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ApplicationStatusDto?> GetCurrentApplicationStatusForApplicantAsync(string applicantUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a summary of the application dashboard for a given application ID.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task<ApplicationDashboardSummaryDto?> GetDashboardSummaryAsync(Guid applicationId);

        /// <summary>
        /// Retrieves full application details including preferences for the edit page
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="applicantUserId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ApplicationDetailsDto?> GetApplicationDetailsAsync(Guid applicationId,
            string applicantUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a paginated list of applications for a given applicant user ID, with optional query parameters for filtering and pagination.
        /// </summary>
        /// <param name="applicantUserId"></param>
        /// <param name="queryParams"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PaginatedResult<ApplicationListDto>> GetMyApplicationsAsync(string applicantUserId,
            QueryParameters queryParams, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the eligibility details for a given application ID and applicant user ID.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="applicantUserId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<EligibilityDetailsDto?> GetEligibilityDetailsAsync(Guid applicationId, string applicantUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the enrollment checklist tasks and progress for a specific application.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="applicantUserId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<EnrollmentChecklistDto?> GetEnrollmentChecklistAsync(Guid applicationId, string applicantUserId, CancellationToken cancellationToken = default);
    }
}
