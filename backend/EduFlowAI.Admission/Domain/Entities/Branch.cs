using System;
using System.Collections.Generic;

namespace EduFlowAI.Admission.Domain.Entities
{
    // Represents a physical location (e.g., Smart Village)
    public sealed class Branch
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Governorate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation property
        public ICollection<TrackBranchOffering> BranchOfferings { get; set; } = new List<TrackBranchOffering>();
    }
}