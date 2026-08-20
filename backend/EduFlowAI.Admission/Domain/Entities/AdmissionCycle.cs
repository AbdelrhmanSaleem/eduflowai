using EduFlowAI.Admission.Domain.Enums;
using System;
using System.Collections.Generic;

namespace EduFlowAI.Admission.Domain.Entities
{
    // Represents a specific intake cycle (e.g., Intake 46)
    public sealed class AdmissionCycle
    {
        public Guid Id { get; set; }
        public Guid ProgramId { get; set; }
        public string Label { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateTimeOffset DeadlineUtc { get; set; }
        public CycleStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ClosedAt { get; set; }

        // PostgreSQL xmin system column used for optimistic concurrency
        public uint RowVersion { get; set; }

        // Navigation properties
        public Program Program { get; set; } = null!;
        public CycleEligibilityRule? EligibilityRule { get; set; }
        public ICollection<TrackBranchOffering> Offerings { get; set; } = new List<TrackBranchOffering>();
    }
}

