using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Infrastructure.DocumentVerification.Gemini;

// Single attempt per call - see IDocumentVerificationGeminiClient. Uses the shared Gemini
// chat model/key config (GeminiOptions): "Every entry must be document-capable (extraction
// shares them)" per that class's own comment.
public sealed class DocumentVerificationGeminiClient : IDocumentVerificationGeminiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public DocumentVerificationGeminiClient(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<string> GenerateVerificationJsonAsync(
        string documentType,
        IReadOnlyDictionary<string, string> expectedFields,
        byte[] fileContent,
        string mimeType,
        CancellationToken cancellationToken)
    {
        var models = _options.ResolvedChatModels;
        var keys = _options.ResolvedKeys;

        if (models.Count == 0 || keys.Count == 0)
        {
            throw new InvalidOperationException("Gemini is not configured (missing chat model or API key).");
        }

        var triedModels = new List<string>();
        var lastStatus = 0;
        var lastDetail = string.Empty;

        foreach (var model in models)
        {
            triedModels.Add(model);
            var modelUnavailable = false;

            foreach (var apiKey in keys)
            {
                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new object[]
                            {
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = mimeType,
                                        data = Convert.ToBase64String(fileContent)
                                    }
                                },
                                new
                                {
                                    text = DocumentVerificationPromptBuilder.BuildInstruction(documentType, expectedFields)
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0,
                        responseMimeType = "application/json",
                        responseSchema = DocumentVerificationPromptBuilder.BuildResponseSchema()
                    }
                };

                for (var attempt = 1; attempt <= 2; attempt++)
                {
                    using var message = new HttpRequestMessage(
                        HttpMethod.Post,
                        $"models/{Uri.EscapeDataString(model)}:generateContent");

                    message.Headers.Add("x-goog-api-key", apiKey);
                    message.Content = JsonContent.Create(payload, options: JsonOptions);

                    using var response = await _httpClient.SendAsync(
                        message,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var envelope = await response.Content.ReadFromJsonAsync<GeminiResponseEnvelope>(
                            JsonOptions,
                            cancellationToken);

                        var json = envelope?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            return json;
                        }
                    }

                    lastStatus = (int)response.StatusCode;
                    lastDetail = await ReadDetailAsync(response, cancellationToken);

                    // 400/404 means this model name is bad/unavailable, move to next model
                    if (lastStatus is 400 or 404)
                    {
                        modelUnavailable = true;
                        break;
                    }

                    if (lastStatus == 429 || lastStatus >= 500)
                    {
                        if (attempt < 2)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
                            continue;
                        }
                        break;
                    }

                    break;
                }

                if (modelUnavailable)
                    break;
            }
        }

        throw new HttpRequestException(
            $"Gemini document verification call failed after trying models [{string.Join(", ", triedModels)}]. " +
            $"Last status HTTP {lastStatus}. {lastDetail}");
    }

    private static async Task<string> ReadDetailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        return detail.Length > 500 ? detail[..500] : detail;
    }

    private sealed record GeminiResponseEnvelope
    {
        public IReadOnlyList<GeminiCandidate>? Candidates { get; init; }
    }

    private sealed record GeminiCandidate
    {
        public GeminiContent? Content { get; init; }
    }

    private sealed record GeminiContent
    {
        public IReadOnlyList<GeminiPart>? Parts { get; init; }
    }

    private sealed record GeminiPart
    {
        public string? Text { get; init; }
    }
}