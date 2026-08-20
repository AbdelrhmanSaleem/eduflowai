namespace EduFlowAI.AI.Application.DTOs;

public sealed record RecommendedTrackResultDto
{
    public required Guid TrackId { get; init; }

    public required string TrackName { get; init; }

    public required int Rank { get; init; }

    public required string Reason { get; init; }
}