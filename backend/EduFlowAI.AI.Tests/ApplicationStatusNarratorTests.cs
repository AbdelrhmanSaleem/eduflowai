using System;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Application.Services;

namespace EduFlowAI.AI.Tests;

// StatusMessage is internal, English-only wording, so the status itself is narrated instead.
public class ApplicationStatusNarratorTests
{
    [Fact]
    public async Task The_real_status_is_handed_to_the_model_as_a_grounded_fact()
    {
        var chat = new StubChat { Response = "Your application is under document verification." };
        var narrator = new ApplicationStatusNarrator(chat);

        var result = await narrator.NarrateAsync(
            "UnderDocumentVerification", DateTimeOffset.UtcNow, "en");

        Assert.Equal("Your application is under document verification.", result);
        Assert.Contains("Under Document Verification", chat.LastSystemPrompt);
        Assert.Contains("Never upgrade, downgrade or soften it", chat.LastSystemPrompt);
    }

    [Theory]
    [InlineData("ar", "Arabic")]
    [InlineData("en", "English")]
    public async Task The_prompt_carries_a_strong_language_directive(string language, string expected)
    {
        var chat = new StubChat();
        var narrator = new ApplicationStatusNarrator(chat);

        await narrator.NarrateAsync("Submitted", DateTimeOffset.UtcNow, language);

        Assert.Contains($"Write your ENTIRE reply in {expected}", chat.LastSystemPrompt);
    }

    [Fact]
    public async Task Mid_conversation_the_prompt_forbids_greeting_again()
    {
        var chat = new StubChat();
        var narrator = new ApplicationStatusNarrator(chat);

        await narrator.NarrateAsync(
            "Submitted", DateTimeOffset.UtcNow, "en", isContinuingConversation: true);

        Assert.Contains("do NOT greet", chat.LastSystemPrompt);
    }

    [Theory]
    [InlineData("en", "Your current application status is: Under Review.")]
    [InlineData("ar", "Under Review")]
    public async Task Model_unavailable_still_reports_the_real_status(string language, string expected)
    {
        var narrator = new ApplicationStatusNarrator(new StubChat { Throw = true });

        var result = await narrator.NarrateAsync("UnderReview", DateTimeOffset.UtcNow, language);

        Assert.Contains(expected, result);
    }

    [Fact]
    public async Task An_empty_model_reply_falls_back_to_the_plain_status()
    {
        var narrator = new ApplicationStatusNarrator(new StubChat { Response = "" });

        var result = await narrator.NarrateAsync("Admitted", DateTimeOffset.UtcNow, "en");

        Assert.Contains("Admitted", result);
    }

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
