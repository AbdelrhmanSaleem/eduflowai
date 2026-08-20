namespace EduFlowAI.AI.Infrastructure.Options;

public sealed class GeminiRecommendationOptions
{
    public const string SectionName = "Gemini:Recommendation";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 30;
}