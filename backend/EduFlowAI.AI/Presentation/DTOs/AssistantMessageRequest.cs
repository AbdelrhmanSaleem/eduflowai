namespace EduFlowAI.AI.Presentation.DTOs;

public sealed record AssistantMessageRequest(
    string Message,
    string? SessionId,
    string? Language,
    RecommendationQuestionnaireProgressDto? Recommendation = null,
    bool RecommendWithAvailableData = false);