using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Infrastructure.ExternalServices;

public class GeminiChatClient : IGeminiChatClient
{
    // Attempts on one (model, key) before moving on - rides out a rate blip or transient 5xx.
    private const int MaxAttemptsPerCombo = 2;

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public GeminiChatClient(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        var request = new GeminiGenerateRequest
        {
            SystemInstruction = new GeminiContent { Parts = new[] { GeminiPart.FromText(systemPrompt) } },
            Contents = new[] { new GeminiContent { Parts = new[] { GeminiPart.FromText(userMessage) } } },
            GenerationConfig = new GeminiGenerationConfig { Temperature = _options.ChatTemperature }
        };

        return SendAsync(request, cancellationToken);
    }

    public Task<string> GenerateJsonAsync(string systemPrompt, string userMessage, object responseSchema, CancellationToken cancellationToken = default)
    {
        var request = new GeminiGenerateRequest
        {
            SystemInstruction = new GeminiContent { Parts = new[] { GeminiPart.FromText(systemPrompt) } },
            Contents = new[] { new GeminiContent { Parts = new[] { GeminiPart.FromText(userMessage) } } },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0,
                ResponseMimeType = "application/json",
                ResponseSchema = responseSchema
            }
        };

        return SendAsync(request, cancellationToken);
    }

    public Task<string> GenerateFromDocumentAsync(string instruction, byte[] document, string mimeType, CancellationToken cancellationToken = default)
    {
        if (document == null || document.Length == 0)
            throw new ArgumentException("Document content cannot be empty.", nameof(document));

        var request = new GeminiGenerateRequest
        {
            Contents = new[]
            {
                new GeminiContent
                {
                    Parts = new[]
                    {
                        GeminiPart.FromInlineData(mimeType, Convert.ToBase64String(document)),
                        GeminiPart.FromText(instruction)
                    }
                }
            },
            GenerationConfig = new GeminiGenerationConfig { Temperature = 0 }
        };

        return SendAsync(request, cancellationToken);
    }

    // Walks each model, then the keys within it, until one combination succeeds. A model has its
    // own quota bucket, so switching model is the real quota lever.
    private async Task<string> SendAsync(GeminiGenerateRequest payload, CancellationToken cancellationToken)
    {
        var models = _options.ResolvedChatModels;
        var keys = _options.ResolvedKeys;
        var triedModels = new List<string>();
        var lastStatus = 0;
        var lastDetail = string.Empty;

        foreach (var model in models)
        {
            triedModels.Add(model);
            var modelUnavailable = false;

            foreach (var key in keys)
            {
                var requestUrl = $"{_options.BaseUrl.TrimEnd('/')}/models/{model}:generateContent?key={key}";

                for (var attempt = 1; attempt <= MaxAttemptsPerCombo; attempt++)
                {
                    var response = await _httpClient.PostAsJsonAsync(requestUrl, payload, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken: cancellationToken);
                        return body?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
                    }

                    lastStatus = (int)response.StatusCode;
                    lastDetail = await ReadDetailAsync(response, cancellationToken);

                    // Unknown/unsupported model: every key answers the same, so skip to the next model.
                    if (lastStatus is 400 or 404)
                    {
                        modelUnavailable = true;
                        break;
                    }

                    // Rate-limited or a server blip: one short backoff, then move to the next key.
                    if (lastStatus == 429 || lastStatus >= 500)
                    {
                        if (attempt < MaxAttemptsPerCombo)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
                            continue;
                        }
                        break;
                    }

                    // Other 4xx (e.g. 401/403 bad or disabled key): this key won't recover, try the next.
                    break;
                }

                if (modelUnavailable)
                    break;
            }
        }

        throw new HttpRequestException(
            $"Gemini generateContent failed after trying models [{string.Join(", ", triedModels)}]. " +
            $"Last status HTTP {lastStatus}. {lastDetail}");
    }

    private static async Task<string> ReadDetailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        return detail.Length > 300 ? detail.Substring(0, 300) : detail;
    }
}

internal sealed class GeminiGenerateRequest
{
    [JsonPropertyName("system_instruction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiContent? SystemInstruction { get; set; }

    [JsonPropertyName("contents")]
    public GeminiContent[] Contents { get; set; } = Array.Empty<GeminiContent>();

    [JsonPropertyName("generationConfig")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiGenerationConfig? GenerationConfig { get; set; }
}

internal sealed class GeminiGenerationConfig
{
    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    [JsonPropertyName("responseMimeType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResponseMimeType { get; set; }

    [JsonPropertyName("responseSchema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ResponseSchema { get; set; }
}

internal sealed class GeminiContent
{
    [JsonPropertyName("parts")]
    public GeminiPart[] Parts { get; set; } = Array.Empty<GeminiPart>();
}

internal sealed class GeminiPart
{
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("inline_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiInlineData? InlineData { get; set; }

    public static GeminiPart FromText(string text) => new() { Text = text };

    public static GeminiPart FromInlineData(string mimeType, string base64Data)
        => new() { InlineData = new GeminiInlineData { MimeType = mimeType, Data = base64Data } };
}

internal sealed class GeminiInlineData
{
    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

internal sealed class GeminiGenerateResponse
{
    [JsonPropertyName("candidates")]
    public GeminiCandidate[]? Candidates { get; set; }
}

internal sealed class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
}
