namespace EduFlowAI.Admission.Application.DTOs
{
    /// <summary>
    /// Lightweight DTO specifically designed for the applicant dashboard list view.
    /// </summary>
    public record ApplicationListDto
    {
        public Guid Id { get; init; }
        public string ProgramName { get; set; }
        public string IntakeName { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTimeOffset? SubmittedAt { get; init; }
    }
}
