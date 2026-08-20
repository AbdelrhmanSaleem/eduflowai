namespace EduFlowAI.Admission.Application.DTOs
{
    /// <summary>
    /// DTO for returning eligibility results including parsed failure reasons.
    /// </summary>
    public record EligibilityDetailsDto
    {
        public bool Passed { get; init; }
        public DateTimeOffset EvaluatedAt { get; init; }
        // Parsed list of rejection reasons, deserialized from the database JSON string
        public List<string> FailureReasons { get; init; } = new();
    }
}
