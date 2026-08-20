namespace EduFlowAI.Admission.Application.DTOs
{
    /// <summary>
    /// Represents a single task in the enrollment checklist for the UI.
    /// </summary>
    public record EnrollmentTaskItemDto
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = string.Empty;

        // Stored as string to easily map to UI colors/icons (e.g., "Pending", "Completed")
        public string Status { get; init; } = string.Empty;

        // Stored as string to let the UI know what action to trigger (e.g., "Signature", "DocumentSubmission")
        public string TaskType { get; init; } = string.Empty;

        // Optional message from the backend (e.g., "Awaiting institutional signature.")
        public string? SubtextMessage { get; init; }

        // Optional URL for external actions like payment gateways or e-signature portals
        public string? ActionUrl { get; init; }
    }
}
