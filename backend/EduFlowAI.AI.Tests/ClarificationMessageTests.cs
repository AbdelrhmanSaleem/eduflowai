using EduFlowAI.AI.Application.Services;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Tests;

public class ClarificationMessageTests
{
    [Fact]
    public void Arabic_question_gets_the_arabic_message()
    {
        Assert.Contains("لم أتأكد", Build().GetClarificationMessage("ar"));
    }

    [Fact]
    public void English_question_gets_the_english_message()
    {
        Assert.StartsWith("I'm not sure I understood", Build().GetClarificationMessage("en"));
    }

    // Anything that isn't Arabic falls back to English rather than returning nothing.
    [Fact]
    public void Unknown_language_falls_back_to_english()
    {
        Assert.StartsWith("I'm not sure I understood", Build().GetClarificationMessage("fr"));
    }

    // It has to name every option, otherwise it isn't a usable clarifying question.
    [Fact]
    public void Message_offers_all_intents()
    {
        var message = Build().GetClarificationMessage("en");

        Assert.Contains("general information", message);
        Assert.Contains("application status", message);
        Assert.Contains("document status", message);
        Assert.Contains("recommendation", message);
    }

    private static IntentClassifierService Build() =>
        new(
            new StubChatClient(),
            Options.Create(new IntentClassificationOptions()),
            NullLogger<IntentClassifierService>.Instance);

    private sealed class StubChatClient : EduFlowAI.AI.Application.Interfaces.IGeminiChatClient
    {
        public System.Threading.Tasks.Task<string> GenerateAsync(string systemPrompt, string userMessage, System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.FromResult(string.Empty);

        public System.Threading.Tasks.Task<string> GenerateJsonAsync(string systemPrompt, string userMessage, object responseSchema, System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.FromResult(string.Empty);

        public System.Threading.Tasks.Task<string> GenerateFromDocumentAsync(string instruction, byte[] document, string mimeType, System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.FromResult(string.Empty);
    }
}
