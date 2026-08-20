using EduFlowAI.Admission.Application.Interfaces.EligibilityRuleStrategy;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Application.Models;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Identity.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EduFlowAI.Admission.Application.Services
{
    public class EligibilityEngine : IEligibilityEngine
    {
        // Inject all strategies registered in the DI container
        private readonly IEnumerable<IEligibilityRuleStrategy> _rules;
        public EligibilityEngine(IEnumerable<IEligibilityRuleStrategy> rules)
        {
            _rules = rules;
        }

        public EligibilityResult EvaluateApplication(Guid applicationId, ApplicantProfile profile, CycleEligibilityRule cycleRule)
        {
            var failureReasons = new List<EligibilityFailureReason>();

            // 1. Evaluate all rules
            foreach(var rule in _rules)
            {
                var failure = rule.Evaluate(profile, cycleRule);
                if(failure != null)
                {
                    failureReasons.Add(failure);
                }
            }

            // 2. Determine the final passed status
            bool isPassed = !(failureReasons.Any());    // Invert the result

            // 3. Serialize the failure reasons to JSON
            string failuresJson = JsonSerializer.Serialize(failureReasons);

            // 4. Create and return the eligibility rules
            return new EligibilityResult
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                Passed = isPassed,
                FailureReasonsJson = failuresJson,
                EvaluatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
