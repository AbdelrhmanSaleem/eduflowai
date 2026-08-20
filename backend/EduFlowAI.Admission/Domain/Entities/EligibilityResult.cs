using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Domain.Entities
{
    public class EligibilityResult
    {
        public Guid Id { get; set; }
        public bool Passed { get; set; }
        /// <summary>
        /// JSON array of failure reasons; "[]" when Passed = true
        /// </summary>
        public string FailureReasonsJson { get; set; } = "[]";
        public DateTimeOffset EvaluatedAt { get; set; }

        public Guid ApplicationId { get; set; }
        public Application Application { get; set; } = null!;
    }
}
