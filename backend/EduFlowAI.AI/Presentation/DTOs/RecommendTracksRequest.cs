namespace EduFlowAI.AI.Presentation.DTOs;

public sealed record RecommendTracksRequest
{
    public required string Major { get; init; }

    public required IReadOnlyCollection<string> TechnicalCourses { get; init; }

    public required IReadOnlyCollection<string> Skills { get; init; }

    public required IReadOnlyCollection<string> Interests { get; init; }

    public required IReadOnlyCollection<string> PreferredActivities { get; init; }

    public required IReadOnlyCollection<string> CareerGoals { get; init; }

    public string? AdditionalContext { get; init; }
}