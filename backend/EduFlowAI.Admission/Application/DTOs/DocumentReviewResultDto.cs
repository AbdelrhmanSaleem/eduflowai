using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Admission.Application.DTOs
{
    public record DocumentReviewResultDto
    {
        public required bool IsAccepted { get; set; }

        // Optional rejection reason, strictly required if IsAccepted is false
        public string? RejectionReason { get; set; }

        // Defines who performed the review (e.g., "AI" or "Human")
        public required ReviewerType ReviewerType { get; set; }

        // True if the AI Agent cannot make a confident decision and needs human intervention
        public bool IsAgentUncertain { get; set; }

        // The ID of the employee who performed the review
        public string? ReviewedByUserId { get; set; }
    }
}
