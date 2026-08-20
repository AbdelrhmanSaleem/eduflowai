using EduFlowAI.Admission.Application.Interfaces.EligibilityRuleStrategy;
using EduFlowAI.Admission.Application.MappingProfiles;
using EduFlowAI.Admission.Application.Models;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Identity.Domain.Entities;

namespace EduFlowAI.Admission.Application.EligibilityRuleStrategy
{
    public class GraduationRecencyRule : IEligibilityRuleStrategy
    {
        public EligibilityFailureReason? Evaluate(ApplicantProfile profile, CycleEligibilityRule cycleRule)
        {
            int yearsSinceGraduation = DateTime.UtcNow.Year - profile.GraduationYear;

            if(yearsSinceGraduation > cycleRule.MaxYearsSinceGraduation)
            {
                return new EligibilityFailureReason
                {
                    Code = "MaximumGraduationYearsExceeded",
                    Message = "The number of years since graduation exceeds the allowed maximum.",
                    ActualValue = yearsSinceGraduation,
                    AllowedValue = cycleRule.MaxYearsSinceGraduation
                };
            }
            return null; // Eligible
        }
    }
}
