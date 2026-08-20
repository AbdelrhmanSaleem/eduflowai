using System;
using System.Collections.Generic;

namespace EduFlowAI.Admission.Domain.Entities
{
    // Seed table representing the organization (e.g., ITI)
    public sealed class Institution
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation Property
        public ICollection<Program> Programs { get; set; } = new List<Program>();
    }
}

