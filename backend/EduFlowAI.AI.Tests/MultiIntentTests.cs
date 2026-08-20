using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Application.Services;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Tests;

public class MultiIntentTests
{
    [Fact]
    public void IntentArray_IsParsedAndRanked()
    {
        var raw = """
        {"intents":[{"intent":"recommendation","confidence":0.7},{"intent":"application_status","confidence":0.9}]}
        """;

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Equal(2, dto.Intents.Count);
        Assert.Equal("application_status", dto.Intents[0].Intent);
        Assert.Equal("recommendation", dto.Intents[1].Intent);
    }

    [Fact]
    public void PrimaryIntent_MirrorsTheTopRankedEntry()
    {
        var raw = """
        {"intents":[{"intent":"knowledge","confidence":0.6},{"intent":"application_status","confidence":0.95}]}
        """;

        IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.Equal("application_status", dto.PrimaryIntent);
        Assert.Equal(0.95m, dto.Confidence);
        Assert.Equal("application_status_service", dto.RoutedTo);
    }

    [Fact]
    public void EveryIntent_CarriesItsOwnRoute()
    {
        var raw = """
        {"intents":[{"intent":"application_status","confidence":0.9},{"intent":"recommendation","confidence":0.8},{"intent":"knowledge","confidence":0.7}]}
        """;

        IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.Equal(
            new[] { "application_status_service", "recommendations_agent", "knowledge_rag" },
            dto.Intents.Select(i => i.RoutedTo));
    }

    [Fact]
    public void DocumentStatus_IsDistinctFromApplicationStatus_AndRoutesToItsOwnService()
    {
        var raw = """
        {"intents":[{"intent":"knowledge","confidence":0.6},{"intent":"document_status","confidence":0.9}]}
        """;

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Equal("document_status", dto.PrimaryIntent);
        Assert.Equal("document_status_service", dto.RoutedTo);
        Assert.Equal("document_status_service", dto.Intents[0].RoutedTo);
    }

    [Fact]
    public void SingleIntent_IsAListOfOne()
    {
        var raw = """{"intents":[{"intent":"knowledge","confidence":0.95}]}""";

        IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.Single(dto.Intents);
        Assert.Equal("knowledge", dto.Intents[0].Intent);
    }

    // Callers written before multi-intent read PrimaryIntent only; that must keep working.
    [Fact]
    public void OldSingleIntentShape_StillParsesAndFillsTheList()
    {
        var raw = """{"primaryIntent":"application_status","confidence":0.9}""";

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Equal("application_status", dto.PrimaryIntent);
        Assert.Single(dto.Intents);
        Assert.Equal("application_status", dto.Intents[0].Intent);
        Assert.Equal("application_status_service", dto.Intents[0].RoutedTo);
    }

    [Fact]
    public void RepeatedIntent_IsCollapsedToTheMostConfident()
    {
        var raw = """
        {"intents":[{"intent":"knowledge","confidence":0.4},{"intent":"knowledge","confidence":0.9}]}
        """;

        IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.Single(dto.Intents);
        Assert.Equal(0.9m, dto.Intents[0].Confidence);
    }

    [Fact]
    public void UnknownIntents_AreDroppedButValidOnesSurvive()
    {
        var raw = """
        {"intents":[{"intent":"billing","confidence":0.9},{"intent":"application_status","confidence":0.8}]}
        """;

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Single(dto.Intents);
        Assert.Equal("application_status", dto.Intents[0].Intent);
    }

    [Fact]
    public void AllIntentsUnknown_FallsBackToKnowledge()
    {
        var raw = """{"intents":[{"intent":"billing","confidence":0.9}]}""";

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.False(ok);
        Assert.Equal("knowledge", dto.PrimaryIntent);
        Assert.Single(dto.Intents);
    }

    [Fact]
    public void EmptyIntentArray_FallsBackToKnowledge()
    {
        var ok = IntentClassifierService.TryParseIntentResponse("""{"intents":[]}""", out var dto);

        Assert.False(ok);
        Assert.Equal("knowledge", dto.PrimaryIntent);
        Assert.Single(dto.Intents);
    }

    [Fact]
    public void OutOfRangeConfidence_IsClamped()
    {
        var raw = """
        {"intents":[{"intent":"application_status","confidence":4.2},{"intent":"knowledge","confidence":-1}]}
        """;

        IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.Equal(1m, dto.Intents[0].Confidence);
        Assert.Equal(0m, dto.Intents[1].Confidence);
    }

    [Fact]
    public async Task WeakSecondaryIntent_IsNotDispatched()
    {
        var raw = """
        {"intents":[{"intent":"knowledge","confidence":0.9},{"intent":"application_status","confidence":0.2}]}
        """;

        var dto = await Classify(Build(raw, minConfidence: 0.6m));

        Assert.Single(dto.Intents);
        Assert.Equal("knowledge", dto.Intents[0].Intent);
    }

    [Fact]
    public async Task StrongSecondaryIntent_IsKept()
    {
        var raw = """
        {"intents":[{"intent":"application_status","confidence":0.9},{"intent":"recommendation","confidence":0.8}]}
        """;

        var dto = await Classify(Build(raw, minConfidence: 0.6m));

        Assert.Equal(2, dto.Intents.Count);
    }

    // A weak primary means "ask the user", not "return nothing to dispatch".
    [Fact]
    public async Task WeakPrimaryIntent_IsKeptAndFlaggedForClarification()
    {
        var raw = """{"intents":[{"intent":"application_status","confidence":0.3}]}""";

        var dto = await Classify(Build(raw, minConfidence: 0.6m));

        Assert.True(dto.RequiresClarification);
        Assert.Single(dto.Intents);
        Assert.Equal("application_status", dto.Intents[0].Intent);
    }

    // A national ID typed into chat must be masked before the text is sent to the model.
    [Fact]
    public async Task LongDigitRuns_AreMaskedBeforeReachingTheModel()
    {
        var client = new FakeChatClient { JsonResponse = """{"intents":[{"intent":"application_status","confidence":0.9}]}""" };
        var svc = new IntentClassifierService(
            client,
            Options.Create(new IntentClassificationOptions()),
            NullLogger<IntentClassifierService>.Instance);

        await svc.ClassifyAsync("my national id is 29801011234567, did I get in?", new List<ConversationTurnDto>());

        Assert.DoesNotContain("29801011234567", client.LastUserMessage);
        Assert.Contains("[hidden]", client.LastUserMessage);
    }

    private sealed class FakeChatClient : IGeminiChatClient
    {
        public string JsonResponse { get; set; } = string.Empty;
        public string? LastUserMessage { get; private set; }

        public Task<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
            => Task.FromResult(string.Empty);

        public Task<string> GenerateJsonAsync(string systemPrompt, string userMessage, object responseSchema, CancellationToken ct = default)
        {
            LastUserMessage = userMessage;
            return Task.FromResult(JsonResponse);
        }

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
}
