using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.Interfaces;

namespace EduFlowAI.AI.Application.Services;

// StatusMessage is internal, English-only wording, so the status itself is narrated instead.
public sealed class ApplicationStatusNarrator : IApplicationStatusNarrator
{
    private readonly IGeminiChatClient _chatClient;

    public ApplicationStatusNarrator(IGeminiChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    public async Task<string> NarrateAsync(
        string currentStatus,
        DateTimeOffset lastUpdatedAt,
        string language,
        bool isContinuingConversation = false,
        CancellationToken cancellationToken = default)
    {
        var readableStatus = Humanize(currentStatus);
        var languageName = language == "ar" ? "Arabic" : "English";

        var greetingRule = isContinuingConversation
            ? "- You are mid-conversation: do NOT greet or introduce the institute again. Answer directly."
            : "- A brief warm opening is fine before the answer.";

        var systemPrompt = $"""
# ROLE
You are the official virtual assistant of the Information Technology Institute (ITI), replying to an
applicant who asked about their application.

# LANGUAGE
Write your ENTIRE reply in {languageName}. The facts below are in English, but you MUST answer only in
{languageName}, translating the status. Never switch languages mid-answer.

# THE FACTS (authoritative - this is the applicant's real record)
- Current application status: {readableStatus}
- Last updated: {lastUpdatedAt:yyyy-MM-dd}

# RULES
- Speak as the institute, first person plural ("we"), in a warm, human, professional tone.
{greetingRule}
- Report exactly this status. Never upgrade, downgrade or soften it, and never invent decisions, dates,
  timelines, next steps or reasons that are not stated above.
- Explain briefly what the status means for the applicant, without promising any outcome.
- Keep it to one short, natural paragraph. No filler, no emoji, no bullet lists.
- Write plain text only. Never use Markdown: no asterisks, underscores, backticks or hash symbols.
""";

        string? answer;
        try
        {
            answer = await _chatClient.GenerateAsync(
                systemPrompt, "What is the status of my application?", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            answer = null; // model unavailable - fall back to the plain fact below
        }

        return string.IsNullOrWhiteSpace(answer)
            ? Fallback(readableStatus, language)
            : answer;
    }

    private static string Fallback(string readableStatus, string language) =>
        language == "ar"
            ? $"حالة طلبك الحالية هي: {readableStatus}."
            : $"Your current application status is: {readableStatus}.";

    // A database value like "UnderDocumentVerification" is spaced so the model can phrase it.
    private static string Humanize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "Unknown";

        var spaced = new StringBuilder(status.Length + 8);

        for (var i = 0; i < status.Length; i++)
        {
            if (i > 0 && char.IsUpper(status[i]) && !char.IsUpper(status[i - 1]))
                spaced.Append(' ');

            spaced.Append(status[i]);
        }

        return spaced.ToString();
    }
}
