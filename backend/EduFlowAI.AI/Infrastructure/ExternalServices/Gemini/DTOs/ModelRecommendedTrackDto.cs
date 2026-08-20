namespace EduFlowAI.AI.Infrastructure.ExternalServices.Gemini.DTOs;

public sealed record ModelRecommendedTrackDto
{
    public required Guid TrackId { get; init; }

    public required int Rank { get; init; }

    public required string Reason { get; init; }
}