using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EduFlowAI.AI.Application.DTOs;

public class IntentClassificationDto
{
    // Every intent found, ranked most likely first. A single-intent message is a list of one.
    [JsonPropertyName("intents")]
    public List<DetectedIntentDto> Intents { get; set; } = new();

    // The top-ranked intent. Kept so a caller that handles one intent still works.
    [JsonPropertyName("primaryIntent")]
    public string PrimaryIntent { get; set; } = string.Empty;  // knowledge, application_status, document_status, recommendation

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }  // 0.0 to 1.0

    // Language the router detected, or null when it gave nothing usable.
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    // The router's English lookup query, or null when it gave nothing usable.
    [JsonPropertyName("searchQuery")]
    public string? SearchQuery { get; set; }

    // Derived from PrimaryIntent by the classifier; never read from the model's JSON.
    [JsonIgnore]
    public string RoutedTo { get; set; } = string.Empty;

    [JsonPropertyName("requiresClarification")]
    public bool RequiresClarification { get; set; }
}
