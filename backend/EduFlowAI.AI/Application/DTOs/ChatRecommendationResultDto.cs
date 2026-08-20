namespace EduFlowAI.AI.Application.DTOs;

public sealed record ChatRecommendationResultDto
{
    public required string Message { get; init; }

    public required bool ShouldUseQuestionnaire { get; init; }

    public IReadOnlyList<RecommendedTrackResultDto> Recommendations { get; init; }
        = [];
}