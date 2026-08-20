using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Application.Services;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Documents.Domain.Enums;

namespace EduFlowAI.AI.Tests;

public class DocumentStatusNarratorTests
{
    [Fact]
    public async Task No_documents_is_narrated_by_the_model_with_the_empty_state_as_a_grounded_fact()
    {
        var chat = new StubChat { Response = "You haven't uploaded any documents yet." };
        var narrator = new DocumentStatusNarrator(chat);

        var result = await narrator.NarrateAsync(new List<ApplicantDocumentDto>(), "en");

        Assert.Equal("You haven't uploaded any documents yet.", result);  // the model phrased it - not canned
        Assert.NotNull(chat.LastSystemPrompt);
        Assert.Contains("not uploaded any documents", chat.LastSystemPrompt);  // the empty state is a grounded fact
    }

    [Fact]
    public async Task Empty_state_falls_back_to_a_plain_line_when_the_model_is_unavailable()
    {
        var narrator = new DocumentStatusNarrator(new StubChat { Throw = true });

        var en = await narrator.NarrateAsync(new List<ApplicantDocumentDto>(), "en");
        var ar = await narrator.NarrateAsync(new List<ApplicantDocumentDto>(), "ar");

        Assert.Contains("haven't uploaded", en);
        Assert.Contains("لا توجد مستندات", ar);
    }

    [Fact]
    public async Task Documents_are_narrated_with_the_exact_statuses_as_grounded_facts()
    {
        var chat = new StubChat { Response = "Your transcript is approved and your ID is under review." };
        var narrator = new DocumentStatusNarrator(chat);

        var result = await narrator.NarrateAsync(Docs(), "en");

        Assert.Equal("Your transcript is approved and your ID is under review.", result);  // model's phrasing wins
        Assert.NotNull(chat.LastSystemPrompt);
        // the authoritative facts are handed to the model verbatim...
        Assert.Contains("Graduation Certificate: Approved", chat.LastSystemPrompt);
        Assert.Contains("National ID: Under manual review", chat.LastSystemPrompt);
        // ...with an instruction never to change them
        Assert.Contains("Never add, change", chat.LastSystemPrompt);
    }

    [Theory]
    [InlineData("ar", "Arabic")]
    [InlineData("en", "English")]
    public async Task The_prompt_carries_a_strong_language_directive(string language, string expected)
    {
        var chat = new StubChat();
        var narrator = new DocumentStatusNarrator(chat);

        await narrator.NarrateAsync(Docs(), language);

        Assert.Contains("# LANGUAGE", chat.LastSystemPrompt);
        Assert.Contains($"Write your ENTIRE reply in {expected}", chat.LastSystemPrompt);
    }

    [Fact]
    public async Task Falls_back_to_the_plain_facts_when_the_model_returns_nothing()
    {
        var chat = new StubChat { Response = "" };
        var narrator = new DocumentStatusNarrator(chat);

        var result = await narrator.NarrateAsync(Docs(), "en");

        // never an empty reply - the deterministic facts still reach the applicant
        Assert.Contains("Graduation Certificate: Approved", result);
        Assert.Contains("National ID: Under manual review", result);
    }

    [Fact]
    public async Task Falls_back_to_the_plain_facts_when_the_model_is_unavailable()
    {
        var chat = new StubChat { Throw = true };   // e.g. quota exhausted
        var narrator = new DocumentStatusNarrator(chat);

        var result = await narrator.NarrateAsync(Docs(), "en");

        Assert.Contains("Graduation Certificate: Approved", result);
        Assert.Contains("National ID: Under manual review", result);
    }

    // This narrator has its own prompt, so it used to welcome the applicant all over again.
    [Fact]
    public async Task Mid_conversation_the_prompt_forbids_greeting_again()
    {
        var chat = new StubChat();
        var narrator = new DocumentStatusNarrator(chat);

        await narrator.NarrateAsync(Docs(), "en", isContinuingConversation: true);

        Assert.Contains("do NOT greet", chat.LastSystemPrompt);
    }

    [Fact]
    public async Task A_new_conversation_may_still_open_warmly()
    {
        var chat = new StubChat();
        var narrator = new DocumentStatusNarrator(chat);

        await narrator.NarrateAsync(Docs(), "en", isContinuingConversation: false);

        Assert.DoesNotContain("do NOT greet", chat.LastSystemPrompt);
        Assert.Contains("warm opening", chat.LastSystemPrompt);
    }

    private static List<ApplicantDocumentDto> Docs() => new()
    {
        new(Guid.NewGuid(), DocumentType.GraduationCertificate, "grad.pdf", DocumentStatus.Approved, DateTimeOffset.UtcNow),
        new(Guid.NewGuid(), DocumentType.NationalId, "id.pdf", DocumentStatus.NeedsHumanReview, DateTimeOffset.UtcNow)
    };

    private sealed class StubChat : IGeminiChatClient
    {
        public string Response { get; set; } = "narrated";
        public bool Throw { get; set; }
        public string? LastSystemPrompt { get; private set; }

        public Task<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
        {
            LastSystemPrompt = systemPrompt;
            if (Throw)
                throw new System.Net.Http.HttpRequestException("model unavailable");
            return Task.FromResult(Response);
        }

        public Task<string> GenerateJsonAsync(string systemPrompt, string userMessage, object responseSchema, CancellationToken ct = default)
            => Task.FromResult(string.Empty);

        public Task<string> GenerateFromDocumentAsync(string instruction, byte[] document, string mimeType, CancellationToken ct = default)
            => Task.FromResult(string.Empty);
    }
}
