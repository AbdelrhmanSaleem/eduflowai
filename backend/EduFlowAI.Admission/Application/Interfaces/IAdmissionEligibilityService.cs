using EduFlowAI.Admission.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Application.Interfaces
{
    public interface IAdmissionEligibilityService
    {
        /// <summary>
        /// Evaluates the eligibility of a specific applicant for a given admission cycle.
        /// </summary>
        /// <param name="applicantId"></param>
        /// <param name="cycleId"></param>
        /// <returns></returns>
        Task<EligibilityResult> EvaluateApplicantAsync(Guid applicantId, Guid cycleId, Guid applicationId);
    }
}
