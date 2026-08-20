namespace EduFlowAI.AI.Application.DTOs;

public sealed record RecommendationUserContextDto
{
    public string? Major { get; init; }

    public string? Faculty { get; init; }

    public string? University { get; init; }

    public string? DegreeLevel { get; init; }

    public int? GraduationYear { get; init; }

    public string? CumulativeGrade { get; init; }

    public bool HasProfileData { get; init; }
}