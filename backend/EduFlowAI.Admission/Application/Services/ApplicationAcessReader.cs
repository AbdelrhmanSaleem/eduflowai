using EduFlowAI.Admission.Application.Interfaces.Repositories;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Identity.Domain.Constants;
using EduFlowAI.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace EduFlowAI.Admission.Application.Services
{
    public class ApplicationAcessReader : IApplicationAcessReader
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly UserManager<AppUser> _userManager;

        public ApplicationAcessReader(IApplicationRepository applicationRepository,
            UserManager<AppUser> userManager)
        {
            _applicationRepository = applicationRepository;
            _userManager = userManager;
        }

        public async Task<bool> CanAccessDocumentsAsync(Guid applicationId, string userId, CancellationToken cancellationToken)
        {
            // Fetch the user from the Identity database using their ID
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Check if the user has the SuperAdmin role
                var isSuperAdmin = await _userManager.IsInRoleAsync(user, AppRoles.SuperAdmin);
                if (isSuperAdmin)
                {
                    return true; // SuperAdmin can upload documents for any application
                }
            }

            // Check if the user is the owner of the application
            var application = await _applicationRepository.GetFirstOrDefaultAsync(app => app.Id == applicationId);
            if(application == null)
            {
                throw new Exception("This application is not found");
            }

            if(application.ApplicantUserId != userId)
            {
                return false;
            }

            return true;    // This user owns this application
        }

        public async Task<Guid> GetProgramIdAsync(Guid applicationId, CancellationToken cancellationToken)
        {
            var application = await _applicationRepository.GetFirstOrDefaultWithIncludeAsync(app => app.Id == applicationId, 
                true, cancellationToken, app => app.Cycle);
            if (application == null)
            {
                return Guid.Empty; // Return an empty GUID if no application is found
            }
            return application.Cycle.ProgramId;
        }

        public async Task<string> GetApplicantUserIdAsync(Guid applicationId, CancellationToken cancellationToken)
        {
            var application = await _applicationRepository.GetFirstOrDefaultAsync(app => app.Id == applicationId);
            if (application == null)
            {
                throw new Exception("This application is not found");
            }
            return application.ApplicantUserId;
        }
        public async Task<Guid> GetApplicationIdForUserAsync(string applicantUserId, CancellationToken cancellationToken)
        {
            // Newest live application, so documents are not read against a withdrawn one.
            var application =
                await _applicationRepository.GetFirstOrDefaultAsync(
                    predicate: app => app.ApplicantUserId == applicantUserId
                                   && app.Status != ApplicationStatus.Withdrawn,
                    include: query => query.OrderByDescending(app => app.CreatedAt))
                ?? await _applicationRepository.GetFirstOrDefaultAsync(
                    predicate: app => app.ApplicantUserId == applicantUserId,
                    include: query => query.OrderByDescending(app => app.CreatedAt));

            if (application == null)
            {
                return Guid.Empty; // Return an empty GUID if no application is found for the user
            }
            return application.Id;
        }
    }
}
