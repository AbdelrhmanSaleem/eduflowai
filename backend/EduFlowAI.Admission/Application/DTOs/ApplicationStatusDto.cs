using EduFlowAI.Admission.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Application.DTOs
{
    public record ApplicationStatusDto
    {
        public Guid ApplicationId { get; set; }

        // The current status of the application (e.g., Submitted, DocumentsRequired, UnderReview)
        public required string CurrentStatus { get; set; }

        public DateTimeOffset LastUpdatedAt { get; set; }

        // Any specific message or reason related to the current status
        public string? StatusMessage { get; set; }
    }
}
