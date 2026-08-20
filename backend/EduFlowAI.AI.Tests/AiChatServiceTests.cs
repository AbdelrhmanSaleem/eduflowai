using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Application.Services;

namespace EduFlowAI.AI.Tests;

public class AiChatServiceTests
{
    private static readonly IReadOnlyList<ConversationTurnDto> NoHistory = new List<ConversationTurnDto>();

    private static readonly IReadOnlyList<ConversationTurnDto> DotNetHistory = new List<ConversationTurnDto>
    {
        new() { Question = "What tracks suit backend developers?", Answer = "The .NET Enterprise track is a strong option." }
    };

    [Fact]
    public async Task First_turn_retrieves_on_the_raw_question()
    {
        var (service, retrieval, _) = Build();

        await service.AnswerWithContextAsync("What tracks are available?", NoHistory, "en");

        Assert.Equal("What tracks are available?", retrieval.LastQuery);
    }

    [Fact]
    public async Task Follow_up_retrieves_on_history_joined_with_the_question()
    {
        var (service, retrieval, _) = Build();

        await service.AnswerWithContextAsync("What about its prerequisites?", DotNetHistory, "en");

        // The topic (.NET) comes from the prior turn; the question is still included.
        Assert.Contains(".NET Enterprise", retrieval.LastQuery);
        Assert.Contains("What about its prerequisites?", retrieval.LastQuery);
    }

    [Fact]
    public async Task Follow_up_puts_the_prior_dialogue_in_the_answer_prompt()
    {
        var (service, _, chat) = Build();

        await service.AnswerWithContextAsync("What about its prerequisites?", DotNetHistory, "en");

        Assert.Contains("CONVERSATION SO FAR", chat.LastSystemPrompt);
        Assert.Contains("What tracks suit backend developers?", chat.LastSystemPrompt);
        Assert.Contains(".NET Enterprise", chat.LastSystemPrompt);
    }

    [Fact]
    public async Task First_turn_adds_no_conversation_section()
    {
        var (service, _, chat) = Build();

        await service.AnswerWithContextAsync("What tracks are available?", NoHistory, "en");

        Assert.DoesNotContain("CONVERSATION SO FAR", chat.LastSystemPrompt);
    }

    // A greeting or an off-book message matches no chunks - the model still answers conversationally,
    // but with no reference material attached (so it chats warmly instead of inventing ITI facts).
    [Fact]
    public async Task No_chunks_still_answers_conversationally_without_reference_material()
    {
        var (service, retrieval, chat) = Build();
        retrieval.Chunks = new List<RetrievedChunkDto>();   // a greeting matches nothing

        var result = await service.AnswerWithContextAsync("hi", NoHistory, "en");

        Assert.Equal("an answer", result.Answer);                          // the model was consulted
        Assert.NotNull(chat.LastSystemPrompt);
        Assert.DoesNotContain("OFFICIAL ITI INFORMATION", chat.LastSystemPrompt);
        Assert.Empty(result.Sources);
    }

    // A national ID typed into chat must not reach the embedding or the model.
    [Fact]
    public async Task Long_digit_runs_are_masked_before_retrieval_and_the_model()
    {
        var (service, retrieval, chat) = Build();

        await service.AnswerWithContextAsync("my national id is 29801011234567", NoHistory, "en");

        Assert.DoesNotContain("29801011234567", retrieval.LastQuery);
        Assert.DoesNotContain("29801011234567", chat.LastUserMessage);
        Assert.Contains("[hidden]", chat.LastUserMessage);
    }

    // The reference material is English, so a single weak instruction let the model drift to English.
    // A prominent LANGUAGE directive naming the target language keeps the answer in the right language.
    [Theory]
    [InlineData("ar", "Arabic")]
    [InlineData("en", "English")]
    public async Task The_prompt_carries_a_strong_language_directive(string language, string expected)
    {
        var (service, _, chat) = Build();

        await service.AnswerWithContextAsync("question", NoHistory, language);

        Assert.Contains("# LANGUAGE", chat.LastSystemPrompt);
        Assert.Contains($"Write your ENTIRE reply in {expected}", chat.LastSystemPrompt);
    }

    // The router's query replaces the history-joined one, so a greeting cannot dominate the vector.
    [Fact]
    public async Task Router_search_query_is_what_gets_retrieved_on()
    {
        var (service, retrieval, _) = Build();

        await service.AnswerWithContextAsync(
            "ايه التراكات المتاحة في الإسكندرية",
            DotNetHistory,
            "ar",
            "tracks offered at Alexandria branch");

        Assert.Equal("tracks offered at Alexandria branch", retrieval.LastQuery);
        Assert.DoesNotContain(".NET Enterprise", retrieval.LastQuery);
    }

    [Fact]
    public async Task Without_a_router_query_history_still_drives_retrieval()
    {
        var (service, retrieval, _) = Build();

        await service.AnswerWithContextAsync("What about its prerequisites?", DotNetHistory, "en");

        Assert.Contains(".NET Enterprise", retrieval.LastQuery);
    }

    // Five chunks could not cover an answer spread across a long reference document.
    [Fact]
    public async Task Retrieval_asks_for_the_configured_number_of_chunks()
    {
        var (service, retrieval, _) = Build();

        await service.AnswerWithContextAsync("What tracks are available?", NoHistory, "en");

        Assert.Equal(10, retrieval.LastLimit);
    }

    // The first turn may welcome the applicant; later turns must not re-introduce the institute.
    [Fact]
    public async Task First_turn_is_allowed_to_greet()
    {
        var (service, _, chat) = Build();

        await service.AnswerWithContextAsync("hi", NoHistory, "en");

        Assert.Contains("Greet the applicant", chat.LastSystemPrompt);
    }

    [Fact]
    public async Task Later_turns_are_told_not_to_greet_again()
    {
        var (service, _, chat) = Build();

        await service.AnswerWithContextAsync("What about its prerequisites?", DotNetHistory, "en");

        Assert.DoesNotContain("Greet the applicant", chat.LastSystemPrompt);
        Assert.Contains("Do NOT greet", chat.LastSystemPrompt);
    }

    // An exhausted quota must not turn into a failed request the applicant sees as a red error.
    [Theory]
    [InlineData("en", "Sorry")]
    [InlineData("ar", "عذراً")]
    public async Task Model_failure_returns_a_friendly_answer_instead_of_throwing(
        string language,
        string expected)
    {
        var retrieval = new FakeRetrieval();
        var chat = new StubChat { Throw = true };
        var service = new AiChatService(retrieval, chat);

        var result = await service.AnswerWithContextAsync("question", NoHistory, language);

        Assert.Contains(expected, result.Answer);
    }

    private static (AiChatService, FakeRetrieval, StubChat) Build()
    {
        var retrieval = new FakeRetrieval();
        var chat = new StubChat();
        return (new AiChatService(retrieval, chat), retrieval, chat);
    }

    private sealed class FakeRetrieval : IKnowledgeRetrievalService
    {
        public string? LastQuery { get; private set; }

        public int LastLimit { get; private set; }

        public List<RetrievedChunkDto> Chunks { get; set; } = new()
        {
            new() { Content = "The .NET Enterprise track runs for nine months.", SourceTitle = "dotnet.md" }
        };

        public Task<List<RetrievedChunkDto>> RetrieveContextAsync(string query, int limit = 5, CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            LastLimit = limit;
            return Task.FromResult(Chunks);
        }
    }

    private sealed class StubChat : IGeminiChatClient
    {
        public string? LastSystemPrompt { get; private set; }
        public string? LastUserMessage { get; private set; }
        public bool Throw { get; set; }

        public Task<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
        {
            LastSystemPrompt = systemPrompt;
            LastUserMessage = userMessage;

            if (Throw)
                throw new System.Net.Http.HttpRequestException("model unavailable");

            return Task.FromResult("an answer");
        }

        public Task<string> GenerateJsonAsync(string systemPrompt, string userMessage, object responseSchema, CancellationToken ct = default)
            => Task.FromResult(string.Empty);

        public Task<string> GenerateFromDocumentAsync(string instruction, byte[] document, string mimeType, CancellationToken ct = default)
            => Task.FromResult(string.Empty);
    }
}
