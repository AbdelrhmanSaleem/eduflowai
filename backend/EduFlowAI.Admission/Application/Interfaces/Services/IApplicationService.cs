using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Shared.Kernel.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Application.Interfaces.Services
{
    public interface IApplicationService
    {
        /// <summary>
        /// Creates a new Draft application for the given applicant user ID and request data.
        /// </summary>
        /// <param name="applicantUserId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<Result<ApplicationDto>> CreateDraftApplicationAsync(string applicantUserId, ApplicationRequestDto request);

        /// <summary>
        /// Updates the preferences of a Draft application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="applicantUserId"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Result<ApplicationDetailsDto>> UpdateApplicationPreferencesAsync(Guid applicationId, string applicantUserId, UpdateApplicationPreferencesDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Submits a Draft application for review.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        Task<Result<ApplicationDetailsDto>> SubmitApplicationAsync(Guid applicationId, string applicantUserId, CancellationToken cancellationToken = default);
    }
}
