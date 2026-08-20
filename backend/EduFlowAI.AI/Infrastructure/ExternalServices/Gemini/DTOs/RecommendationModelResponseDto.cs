namespace EduFlowAI.AI.Infrastructure.ExternalServices.Gemini.DTOs;

public sealed record RecommendationModelResponseDto
{
    public required IReadOnlyCollection<ModelRecommendedTrackDto>
        Recommendations
    { get; init; }
}