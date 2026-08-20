using EduFlowAI.Admission.Domain.Enums;
using System;
using System.Collections.Generic;

namespace EduFlowAI.Admission.Domain.Entities
{
    // Dynamically defines which documents are required for a specific program, optionally scoped by gender
    public sealed class ProgramDocumentRequirement
    {
        public Guid Id { get; set; }

        public Guid ProgramId { get; set; }

        public DocumentType DocumentType { get; set; }

        // Null means required for all genders. If specified, required only for that gender.
        public Gender? RequiredForGender { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation property
        public Program Program { get; set; } = null!;
    }
}
