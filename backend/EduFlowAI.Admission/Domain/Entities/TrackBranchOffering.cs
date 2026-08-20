using System;

namespace EduFlowAI.Admission.Domain.Entities
{
    // The intersection: Which track is offered in which branch during which cycle, and its capacity
    public sealed class TrackBranchOffering
    {
        public Guid Id { get; set; }
        public Guid CycleId { get; set; }
        public Guid TrackId { get; set; }
        public Guid BranchId { get; set; }
        public int Capacity { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public AdmissionCycle Cycle { get; set; } = null!;
        public Track Track { get; set; } = null!;
        public Branch Branch { get; set; } = null!;
        public ICollection<ApplicationPreference> ApplicationPreferences { get; set; } = new List<ApplicationPreference>();
    }
}