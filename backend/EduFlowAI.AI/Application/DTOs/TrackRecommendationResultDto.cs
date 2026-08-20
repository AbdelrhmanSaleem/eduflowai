namespace EduFlowAI.AI.Application.DTOs;

public sealed record TrackRecommendationResultDto
{
    public required IReadOnlyList<RecommendedTrackResultDto> Recommendations { get; init; }

    public required bool UsedFallback { get; init; }

    public required string AdvisoryNotice { get; init; }
}