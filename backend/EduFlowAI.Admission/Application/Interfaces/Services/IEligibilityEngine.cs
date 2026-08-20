using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Identity.Domain.Entities;

namespace EduFlowAI.Admission.Application.Interfaces.Services
{
    public interface IEligibilityEngine
    {
        /// <summary>
        /// Evaluates the eligibility rules for the applicant's application and returns the eligibility results.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="profile"></param>
        /// <param name="cycleRule"></param>
        /// <returns></returns>
        EligibilityResult EvaluateApplication(Guid applicationId, ApplicantProfile profile,
            CycleEligibilityRule cycleRule);
    }
}
