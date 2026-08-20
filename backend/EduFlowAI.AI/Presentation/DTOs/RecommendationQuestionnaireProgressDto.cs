namespace EduFlowAI.AI.Presentation.DTOs;

public sealed record RecommendationQuestionnaireProgressDto
{
    public string? Major { get; init; }

    public IReadOnlyCollection<string>? TechnicalCourses { get; init; }

    public IReadOnlyCollection<string>? Skills { get; init; }

    public IReadOnlyCollection<string>? Interests { get; init; }

    public IReadOnlyCollection<string>? PreferredActivities { get; init; }

    public IReadOnlyCollection<string>? CareerGoals { get; init; }

    public string? AdditionalContext { get; init; }

    public IReadOnlyCollection<string> SkippedFields { get; init; }
        = [];
}