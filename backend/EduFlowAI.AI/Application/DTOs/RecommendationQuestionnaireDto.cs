namespace EduFlowAI.AI.Application.DTOs;

public sealed record RecommendationQuestionnaireDto
{
    public string? Major { get; init; }

    public IReadOnlyCollection<string> TechnicalCourses { get; init; }
        = [];

    public IReadOnlyCollection<string> Skills { get; init; }
        = [];

    public IReadOnlyCollection<string> Interests { get; init; }
        = [];

    public IReadOnlyCollection<string> PreferredActivities { get; init; }
        = [];

    public IReadOnlyCollection<string> CareerGoals { get; init; }
        = [];

    public string? AdditionalContext { get; init; }
}