namespace EduFlowAI.AI.Presentation.DTOs;

public sealed record RecommendTracksResponse
{
    public required IReadOnlyList<RecommendedTrackResponse>
        Recommendations
    { get; init; }

    public required bool UsedFallback { get; init; }

    public required string AdvisoryNotice { get; init; }
}