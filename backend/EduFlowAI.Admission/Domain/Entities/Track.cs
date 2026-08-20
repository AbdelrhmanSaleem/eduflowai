using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Domain.Entities
{
    // Represents a specialization within a program (e.g., Artificial Intelligence, CRM)
    public sealed class Track
    {
        public Guid Id { get; set; }

        public Guid ProgramId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        // Will be mapped to JSONB in PostgreSQL
        public List<string> PrerequisiteTopics { get; set; } = new List<string>();

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public Program Program { get; set; } = null!;
        public ICollection<TrackBranchOffering> BranchOfferings { get; set; } = new List<TrackBranchOffering>();
    }
}