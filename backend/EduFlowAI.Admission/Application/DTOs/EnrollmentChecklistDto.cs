namespace EduFlowAI.Admission.Application.DTOs
{
    /// <summary>
    /// Represents the full enrollment checklist dashboard for an applicant.
    /// </summary>
    public record EnrollmentChecklistDto
    {
        // Used to calculate and display the progress bar in the UI
        public int CompletedTasksCount { get; init; }

        public int TotalTasksCount { get; init; }

        // The actual list of tasks to be rendered
        public List<EnrollmentTaskItemDto> Tasks { get; init; } = new List<EnrollmentTaskItemDto>();
    }
}
