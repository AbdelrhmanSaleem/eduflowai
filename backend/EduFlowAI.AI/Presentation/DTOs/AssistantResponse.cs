namespace EduFlowAI.AI.Presentation.DTOs;

public sealed record AssistantResponse(
    string SessionId,
    bool RequiresClarification,
    string? ClarificationMessage,
    IReadOnlyList<AssistantResultDto> Results,
    DateTimeOffset Timestamp);