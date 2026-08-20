using EduFlowAI.Admission.Application.Interfaces.EligibilityRuleStrategy;
using EduFlowAI.Admission.Application.Models;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Identity.Domain.Entities;
using EduFlowAI.Identity.Domain.Enums;

namespace EduFlowAI.Admission.Application.EligibilityRuleStrategy
{
    public class MilitaryStatusRule : IEligibilityRuleStrategy
    {
        public EligibilityFailureReason? Evaluate(ApplicantProfile profile, CycleEligibilityRule cycleRule)
        {
            if(profile.Gender == Gender.Male)
            {
                var documentRequirements = cycleRule.Cycle?.Program?.DocumentRequirements;
                
                if(documentRequirements != null)
                {
                    bool requiresMilitaryCert = documentRequirements.Any(req =>
                        req.DocumentType == Domain.Enums.DocumentType.MilitaryCertificate &&
                        (req.RequiredForGender == Domain.Enums.Gender.Male || req.RequiredForGender == null));

                    if (requiresMilitaryCert)
                    {
                        if(profile.MilitaryStatus != MilitaryStatus.Completed &&
                           profile.MilitaryStatus != MilitaryStatus.Exempted)
                        {
                            return new EligibilityFailureReason
                            {
                                Code = "MilitaryStatusNotCompleted",
                                Message = "Applicant's military status is not completed.",
                                ActualValue = profile.MilitaryStatus?.ToString() ?? "Missing",
                                AllowedValue = "Completed or Exempted"
                            };
                        }
                    }
                }

                // Fallback: Ensure they provided a status at all
                if (profile.MilitaryStatus == null)
                {
                    return new EligibilityFailureReason
                    {
                        Code = "MilitaryStatusMissing",
                        Message = "Male applicants must provide a valid military status.",
                        ActualValue = "Missing",
                        AllowedValue = "Any valid status"
                    };
                }
            }

            return null; // Eligible
        }
    }
}
