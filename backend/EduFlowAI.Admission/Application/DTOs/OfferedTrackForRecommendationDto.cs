namespace EduFlowAI.Admission.Application.DTOs;
public sealed record OfferedTrackForRecommendationDto
{
    public required Guid TrackId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required IReadOnlyCollection<string> PrerequisiteTopics { get; init; }

    public string? Category { get; init; }

    public string? MinimumGrade { get; init; }

    public string? EligibilitySummary { get; init; }

    public int? TotalHours { get; init; }

    public int? GraduationYearLimitYears { get; init; }

    public required IReadOnlyCollection<string> Locations { get; init; }
}
