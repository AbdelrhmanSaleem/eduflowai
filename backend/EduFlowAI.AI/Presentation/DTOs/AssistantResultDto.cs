namespace EduFlowAI.AI.Presentation.DTOs;

public sealed record AssistantResultDto(
    string Intent,
    string Title,
    string Content,
    IReadOnlyList<string> Sources,
    IReadOnlyDictionary<string, object?> Metadata);