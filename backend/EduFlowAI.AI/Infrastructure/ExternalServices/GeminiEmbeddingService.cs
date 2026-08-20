using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Infrastructure.Caching;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Infrastructure.ExternalServices;

public class GeminiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly EmbeddingCacheService _cache;
    private readonly ILogger<GeminiEmbeddingService> _logger;

    public GeminiEmbeddingService(HttpClient httpClient, IOptions<GeminiOptions> options, EmbeddingCacheService cache, ILogger<GeminiEmbeddingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text content cannot be null or empty when generating an embedding.", nameof(text));


        if (_cache.TryGetEmbedding(text, out var cachedEmbedding))
        {
            return cachedEmbedding!;
        }

        // No model fallback: a different model is a different vector space. One key; quota
        // exhaustion here surfaces as a failed ingestion to retry.
        string apiKey = _options.ResolvedKeys[0];
        string requestUrl = $"{_options.BaseUrl.TrimEnd('/')}/{_options.EmbeddingModel}:embedContent?key={apiKey}";

        var payload = new
        {
            model = _options.EmbeddingModel,
            content = new
            {
                parts = new[]
                {
                    new { text = text }
                }
            },
            outputDimensionality = _options.EmbeddingDimensions
        };

        // A blip just retries; a 429 waits for the quota window to roll over.
        const int maxAttempts = 5;
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(requestUrl, payload, cancellationToken);

                if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < maxAttempts)
                {
                    var wait = RetryAfter(response) ?? TimeSpan.FromSeconds(Math.Pow(3, attempt));

                    _logger.LogWarning(
                        "Embedding rate-limited (attempt {Attempt}); waiting {DelaySeconds}s before retrying.",
                        attempt, (int)wait.TotalSeconds);

                    await Task.Delay(wait, cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<GeminiResponseContract>(cancellationToken: cancellationToken);

                if (result?.Embedding?.Values == null)
                {
                    throw new InvalidOperationException("The Gemini API returned a successful status code, but the embedding payload was empty or malformed.");
                }

                _cache.CacheEmbedding(text, result.Embedding.Values);

                return result.Embedding.Values;
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Embedding attempt {Attempt} failed; retrying in {DelaySeconds}s", attempt, attempt * 2);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Failed to generate embedding after {maxAttempts} attempts due to a network error.", lastException);
    }

    // Gemini tells us how long the quota window has left; obeying it beats guessing.
    private static TimeSpan? RetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
            return delta;

        if (retryAfter?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero)
                return wait;
        }

        return null;
    }

    #region Internal JSON Contract Classes

    private class GeminiResponseContract
    {
        [JsonPropertyName("embedding")]
        public EmbeddingData Embedding { get; set; } = null!;
    }

    private class EmbeddingData
    {
        [JsonPropertyName("values")]
        public float[] Values { get; set; } = null!;
    }

    #endregion
}