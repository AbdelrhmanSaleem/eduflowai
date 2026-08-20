using EduFlowAI.Admission.Domain.Enums;
using System;

namespace EduFlowAI.Admission.Domain.Entities
{
    // The specific rules applied to an admission cycle
    public sealed class CycleEligibilityRule
    {
        public Guid Id { get; set; }

        public Guid CycleId { get; set; }

        public string RequiredNationality { get; set; } = "EGY";

        public string RequiredDegreeLevel { get; set; } = "Bachelor";

        public int MaxYearsSinceGraduation { get; set; } = 5;

        public CumulativeGrade MinGrade { get; set; } = CumulativeGrade.Good;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation property (1-to-1)
        public AdmissionCycle Cycle { get; set; } = null!;
    }
}