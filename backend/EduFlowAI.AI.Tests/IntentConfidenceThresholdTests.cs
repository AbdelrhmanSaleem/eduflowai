using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Application.Services;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Tests;

public class IntentConfidenceThresholdTests
{
    private sealed class FakeChatClient : IGeminiChatClient
    {
        public string JsonResponse { get; set; } = string.Empty;

        public Task<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
            => Task.FromResult(string.Empty);

        public Task<string> GenerateJsonAsync(string systemPrompt, string userMessage, object responseSchema, CancellationToken ct = default)
            => Task.FromResult(JsonResponse);

        public Task<string> GenerateFromDocumentAsync(string instruction, byte[] document, string mimeType, CancellationToken ct = default)
            => Task.FromResult(string.Empty);
    }

    private static IntentClassifierService Build(string jsonResponse, decimal minConfidence = 0.6m) =>
        new(
            new FakeChatClient { JsonResponse = jsonResponse },
            Options.Create(new IntentClassificationOptions { MinConfidence = minConfidence }),
            NullLogger<IntentClassifierService>.Instance);

    private static Task<IntentClassificationDto> Classify(IntentClassifierService svc) =>
        svc.ClassifyAsync("some question", new List<ConversationTurnDto>());

    [Fact]
    public async Task HighConfidence_RoutesWithoutClarification()
    {
        var svc = Build("""{"primaryIntent":"knowledge","confidence":0.95}""");

        var result = await Classify(svc);

        Assert.Equal("knowledge", result.PrimaryIntent);
        Assert.Equal("knowledge_rag", result.RoutedTo);
        Assert.False(result.RequiresClarification);
    }

    [Fact]
    public async Task LowConfidence_AsksForClarification()
    {
        var svc = Build("""{"primaryIntent":"application_status","confidence":0.3}""");

        var result = await Classify(svc);

        Assert.True(result.RequiresClarification); // ambiguous -> ask instead of guessing
        Assert.Equal("application_status", result.PrimaryIntent);
    }

    [Fact]
    public async Task ConfidenceExactlyAtThreshold_IsAccepted()
    {
        var svc = Build("""{"primaryIntent":"recommendation","confidence":0.6}""", minConfidence: 0.6m);

        var result = await Classify(svc);

        Assert.False(result.RequiresClarification);
        Assert.Equal("recommendations_agent", result.RoutedTo);
    }

    [Fact]
    public async Task ThresholdIsConfigurable()
    {
        // The same 0.7 response is confident enough at 0.6 but not at 0.9.
        Assert.False((await Classify(Build("""{"primaryIntent":"knowledge","confidence":0.7}""", 0.6m))).RequiresClarification);
        Assert.True((await Classify(Build("""{"primaryIntent":"knowledge","confidence":0.7}""", 0.9m))).RequiresClarification);
    }

    [Fact]
    public async Task UnparseableResponse_AnswersFromKnowledgeWithoutClarificationLoop()
    {
        var svc = Build("the model said something unexpected");

        var result = await Classify(svc);

        Assert.Equal("knowledge", result.PrimaryIntent);
        Assert.False(result.RequiresClarification);
    }
}
