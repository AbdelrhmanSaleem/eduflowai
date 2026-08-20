using System.Net.Http.Json;
using System.Text.Json;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Infrastructure.ExternalServices.Gemini.DTOs;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Infrastructure.ExternalServices.Gemini;

public sealed class GeminiRecommendationModelClient(
    HttpClient httpClient,
    IOptions<GeminiRecommendationOptions> options,
    ILogger<GeminiRecommendationModelClient> logger)
    : IRecommendationModelClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly GeminiRecommendationOptions _options = options.Value;

    public async Task<RecommendationModelResponseDto> RankAsync(
        RecommendationModelRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        var endpoint =
            $"v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent";

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            endpoint);

        message.Headers.Add(
            "x-goog-api-key",
            _options.ApiKey);

        message.Content = JsonContent.Create(
            CreatePayload(request),
            options: JsonOptions);

        using var response = await httpClient.SendAsync(
            message,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var rawResponse =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Gemini recommendation request failed with status {StatusCode}. Response: {Response}",
                (int)response.StatusCode,
                rawResponse);

            response.EnsureSuccessStatusCode();
        }

        logger.LogInformation(
            "Gemini recommendation raw response: {Response}",
            rawResponse);

        var envelope =
            JsonSerializer.Deserialize<GeminiResponseEnvelope>(
                rawResponse,
                JsonOptions);

        var json = envelope?
            .Candidates?
            .FirstOrDefault()?
            .Content?
            .Parts?
            .FirstOrDefault()?
            .Text;

        if (string.IsNullOrWhiteSpace(json))
        {
            logger.LogWarning(
                "Gemini recommendation response contained no candidate text.");

            throw new InvalidOperationException(
                "Gemini returned an empty recommendation response.");
        }

        logger.LogInformation(
            "Gemini recommendation candidate JSON: {Json}",
            json);

        try
        {
            return JsonSerializer.Deserialize<RecommendationModelResponseDto>(
                       json,
                       JsonOptions)
                   ?? throw new JsonException(
                       "Gemini returned an invalid recommendation response.");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Failed to deserialize Gemini recommendation candidate JSON: {Json}",
                json);

            throw;
        }
    }

    private static object CreatePayload(
        RecommendationModelRequestDto request)
    {
        return new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new
                        {
                            text = RecommendationPromptBuilder.Build(request)
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        recommendations = new
                        {
                            type = "array",
                            maxItems = 3,
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    trackId = new
                                    {
                                        type = "string"
                                    },
                                    rank = new
                                    {
                                        type = "integer"
                                    },
                                    reason = new
                                    {
                                        type = "string"
                                    }
                                },
                                required = new[]
                                {
                                    "trackId",
                                    "rank",
                                    "reason"
                                }
                            }
                        }
                    },
                    required = new[]
                    {
                        "recommendations"
                    }
                }
            }
        };
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Gemini recommendation API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException(
                "Gemini recommendation model is not configured.");
        }
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