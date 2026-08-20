using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Documents.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using EduFlowAI.Admission.Domain.Enums;


namespace EduFlowAI.Documents.Domain.Entities
{
    public class ApplicantDocument
    {
        public Guid Id { get; set; }

        public Guid ApplicationId { get; set; }

        public DocumentType DocumentType { get; set; }
        public string StorageKey { get; set; } = default!;
        public string OriginalFileName { get; set; } = default!;
        public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
        public string? VerificationDetailsJson { get; set; }
        public string? RejectionReason { get; set; }
        public DateTimeOffset? VerifiedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<DocumentReplacementRequest> ReplacementRequests { get; set; } = new List<DocumentReplacementRequest>();
        public ICollection<ApplicantDocumentVersion> Versions { get; set; } = new List<ApplicantDocumentVersion>();
    }
}
