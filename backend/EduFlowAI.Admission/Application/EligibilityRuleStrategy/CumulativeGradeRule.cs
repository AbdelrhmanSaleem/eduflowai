using EduFlowAI.Admission.Application.Interfaces.EligibilityRuleStrategy;
using EduFlowAI.Admission.Application.Models;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Identity.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Application.EligibilityRuleStrategy
{
    public class CumulativeGradeRule : IEligibilityRuleStrategy
    {
        /// <summary>
        /// // Dictionary to hold the ordinal values of the grades
        /// </summary>
        private static readonly Dictionary<string, int> GradeOrdinals = new Dictionary<string, int>
        {
            { "Acceptable", 1 },
            { "Good", 2 },
            { "VeryGood", 3 },
            { "Excellent", 4 }
        };

        public EligibilityFailureReason? Evaluate(ApplicantProfile profile, CycleEligibilityRule cycleRule)
        {
            // Try to get the ordinal values for both the profile grade and the minimum required grade
            if(GradeOrdinals.TryGetValue(profile.CumulativeGrade.ToString(), out int applicantGradeOrdinal) && 
               GradeOrdinals.TryGetValue(cycleRule.MinGrade.ToString(), out int minGradeOrdinal))
            {
                // Check if the applicant's grade ordinal is less than the minimum required
                if(applicantGradeOrdinal < minGradeOrdinal)
                {
                    return new EligibilityFailureReason
                    {
                        Code = "MinimumGradeNotMet",
                        Message = "The cumulative grade does not meet the minimum requirement.",
                        ActualValue = profile.CumulativeGrade,
                        AllowedValue = cycleRule.MinGrade
                    };
                }
            }

            // Rule passed or invalid grade format (handled by DB constraints)
            return null;
        }
    }
}
