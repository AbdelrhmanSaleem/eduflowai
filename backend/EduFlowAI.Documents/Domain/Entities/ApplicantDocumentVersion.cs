using EduFlowAI.Documents.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Domain.Entities
{
    public class ApplicantDocumentVersion
    {
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public int VersionNumber { get; set; }

        public string StorageKey { get; set; } = default!;

        public string OriginalFileName { get; set; } = default!;

        public DocumentStatus Status { get; set; }

        public string? VerificationDetailsJson { get; set; }

        public string? RejectionReason { get; set; }

        public DateTimeOffset? VerifiedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public ApplicantDocument Document { get; set; } = null!;
    }
}
