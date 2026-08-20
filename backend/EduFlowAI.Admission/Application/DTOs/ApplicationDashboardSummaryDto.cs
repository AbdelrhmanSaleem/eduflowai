namespace EduFlowAI.Admission.Application.DTOs
{
    public record ApplicationDashboardSummaryDto
    {
        // Adding the ID back to be used internally by the frontend code
        public Guid ApplicationId { get; init; }

        // Display fields for the dashboard header
        public string IntakeName { get; init; } = string.Empty;
        public DateTimeOffset? SubmittedAt { get; init; }
        public DateTimeOffset LastUpdatedAt { get; init; }

        // Overall application status
        public string CurrentStatus { get; init; } = string.Empty;

        // Eligibility summary
        public string EligibilityResult { get; init; } = "Pending";

        // A descriptive message for the user based on status
        public string StatusMessage { get; init; } = string.Empty;

        // --- Fields for Final Result Outcomes ---
        public string? TrackName { get; init; }
        public string? BranchName { get; init; }
        public int? WaitlistPosition { get; init; }

        // Progress percentage for the timeline bar (0 to 100)
        public int TimelineProgressPercentage { get; init; }

        // Individual status for each of the 7 stages (Expected values: "Passed", "InProgress", "Pending")
        public string ApplicationPhaseStatus { get; init; } = "Pending";
        public string EligibilityPhaseStatus { get; init; } = "Pending";
        public string VerificationPhaseStatus { get; init; } = "Pending";
        public string EnglishIqPhaseStatus { get; init; } = "Pending";
        public string TechnicalPhaseStatus { get; init; } = "Pending";
        public string InterviewPhaseStatus { get; init; } = "Pending";
        public string FinalResultPhaseStatus { get; init; } = "Pending";
    }
}
