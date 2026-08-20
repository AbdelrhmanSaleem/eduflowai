using EduFlowAI.Admission.Application.Models;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Identity.Domain.Entities;

namespace EduFlowAI.Admission.Application.Interfaces.EligibilityRuleStrategy
{
    /// <summary>
    /// Strategy interface for evaluating a single eligibility rule.
    /// </summary>
    public interface IEligibilityRuleStrategy
    {
        /// <summary>
        /// Evaluates the rule and returns a failure reason if it fails, or null if it passes.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="cycleRule"></param>
        /// <returns></returns>
        EligibilityFailureReason? Evaluate(ApplicantProfile profile, CycleEligibilityRule cycleRule);
    }
}
