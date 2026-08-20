using EduFlowAI.Admission.Application.Interfaces.EligibilityRuleStrategy;
using EduFlowAI.Admission.Application.Models;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Identity.Domain.Entities;

namespace EduFlowAI.Admission.Application.EligibilityRuleStrategy
{
    public class NationalityRule : IEligibilityRuleStrategy
    {
        public EligibilityFailureReason? Evaluate(ApplicantProfile profile, CycleEligibilityRule cycleRule)
        {
            if (profile.Nationality != cycleRule.RequiredNationality)
            {
                return new EligibilityFailureReason
                {
                    Code = "NationalityNotAllowed",
                    Message = $"The applicant's nationality ({profile.Nationality}) does not meet the required nationality ({cycleRule.RequiredNationality})",
                    ActualValue = profile.Nationality,
                    AllowedValue = cycleRule.RequiredNationality
                };
            }

            return null;
        }
    }
}
