using System.Collections.Generic;
using System.Linq;

namespace EduFlowAI.AI.Infrastructure.Options;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    // Single-value config kept for back-compat; the lists below take precedence when set.
    public string ApiKey { get; set; } = string.Empty;

    // Extra keys (separate projects) tried in order on quota/auth failures. Empty falls back to ApiKey.
    //public List<string> ApiKeys { get; set; } = new();
    public List<string> ApiKeys { get; set; } = new();

    public string EmbeddingModel { get; set; } = "models/gemini-embedding-001";

    // Must match the vector column width; changing it needs a migration + full re-sync. No model
    // fallback for embeddings - a different model is a different vector space.
    public int EmbeddingDimensions { get; set; } = 1536;

    // gemini-2.5-flash returns 404 for newly issued keys.
    public string ChatModel { get; set; } = "gemini-3.1-flash-lite";

    // Tried in order on rate-limit/quota/unavailable - each model has its own quota bucket, the
    // main quota lever. Empty falls back to ChatModel; every entry must be document-capable.
    public List<string> ChatModels { get; set; } = new();

    public double ChatTemperature { get; set; } = 0.2;

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";

    public IReadOnlyList<string> ResolvedKeys =>
        ApiKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToList() is { Count: > 0 } keys
            ? keys
            : new List<string> { ApiKey };

    public IReadOnlyList<string> ResolvedChatModels =>
        ChatModels.Where(m => !string.IsNullOrWhiteSpace(m)).ToList() is { Count: > 0 } models
            ? models
            : new List<string> { ChatModel };
}
