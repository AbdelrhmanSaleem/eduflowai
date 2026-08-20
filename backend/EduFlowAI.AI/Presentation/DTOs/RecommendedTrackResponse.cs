namespace EduFlowAI.AI.Presentation.DTOs;

public sealed record RecommendedTrackResponse
{
    public required Guid TrackId { get; init; }

    public required string TrackName { get; init; }

    public required int Rank { get; init; }

    public required string Reason { get; init; }
}