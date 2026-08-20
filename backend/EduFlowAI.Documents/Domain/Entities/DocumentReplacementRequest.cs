using EduFlowAI.Documents.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Domain.Entities
{
    public class DocumentReplacementRequest
    {
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public string RequestedByUserId { get; set; }

        public string Reason { get; set; } = default!;
        public ReplacementRequestStatus Status { get; set; } = ReplacementRequestStatus.Open;
        public DateTimeOffset RequestedAt { get; set; }
        public DateTimeOffset? FulfilledAt { get; set; }

        public ApplicantDocument Document { get; set; } = null!;
    }
}
