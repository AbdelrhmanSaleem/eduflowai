using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Application.Services;

public class IntentClassifierService : IIntentRouter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Long enough for a resolved, self-contained query; anything beyond this is the model rambling.
    private const int MaxSearchQueryLength = 300;

    private static readonly string[] ValidIntents = { "knowledge", "application_status", "document_status", "recommendation" };

    // The enum constraint makes an out-of-vocabulary intent impossible.
    private static readonly object ResponseSchema = new
    {
        type = "OBJECT",
        properties = new
        {
            intents = new
            {
                type = "ARRAY",
                items = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        intent = new { type = "STRING", @enum = new[] { "knowledge", "application_status", "document_status", "recommendation" } },
                        confidence = new { type = "NUMBER" },
                        reason = new { type = "STRING" }
                    },
                    required = new[] { "intent", "confidence" }
                }
            },
            // The language the message is written in - the assistant answers in this language.
            language = new { type = "STRING", @enum = new[] { "ar", "en" } },
            // A self-contained English query for the document lookup ("" when none is needed).
            searchQuery = new { type = "STRING" }
        },
        required = new[] { "intents", "language", "searchQuery" }
    };

    private const string SystemPrompt = """
You are the intent router for the Information Technology Institute (ITI) admission assistant.
Identify every intent the user's message asks for, ranked with the most likely one first.

Most messages contain exactly one intent - return a list of one. Only return more than one when the
message genuinely asks for separate things that need separate answers. Never invent an intent the
user did not ask for, and never repeat the same intent twice.

Decide by what the user WANTS, never by which keywords appear:

knowledge - objective, public facts about ITI: programs, which tracks exist, what a track covers,
admission requirements, eligibility, grades, fees, dates, locations, the selection process and how
to apply. The answer is identical for every applicant and comes from official documents. Listing,
describing or explaining things belongs here. Greetings, thanks and general small talk also belong
here - the assistant replies conversationally - so classify a clear hello or thank-you as knowledge
with HIGH confidence, not as something vague.

application_status - the state or outcome of THIS user's own application: whether they were accepted,
rejected or waitlisted, and how far their application has progressed.

document_status - the state of THIS user's uploaded documents: whether they were received, are under
verification, were approved or rejected, and which documents are still required.

recommendation - asks the assistant to CHOOSE or ADVISE which option suits THIS user personally,
based on their background, interests, skills or preferences.

Critical rule: asking WHICH TRACKS EXIST is knowledge. Asking WHICH TRACK SUITS ME is
recommendation. The word "track" never decides the intent by itself - the user's goal does.

Disambiguation: the overall application outcome is application_status; the state of the uploaded
files is document_status.

Set each confidence honestly. Use a low value when the message is vague or you cannot tell which
intent it belongs to - but a clear greeting, thanks or bit of small talk is confidently knowledge,
not low confidence. A message that is ambiguous between two intents is NOT a
multi-intent message - it is one intent with low confidence. Only list several intents when the user
clearly asks for several things.

Also detect the language the CURRENT message is written in and return it as "language": "ar" for Arabic,
"en" for English or anything else. Judge by the sentence the user actually wrote, not by the conversation
history. A mostly-Arabic sentence that contains English tech terms, job titles or proper nouns (for
example "اني Product Manager" or "عايز اشتغل Backend Developer") is Arabic -> "ar". Only return "en" when
the message is genuinely English, or has no real words to judge (e.g. only digits or symbols).

Finally, write "searchQuery": a short, self-contained search query IN ENGLISH describing what the user
wants to look up in ITI's official documents. Always write it in English even when the message is Arabic,
because the documents are English. Resolve references from the conversation ("its prerequisites" ->
"<track name> prerequisites"). Keep the specific nouns that matter - branch or city names, track names,
requirement names. Never include greetings, thanks or small talk. Return an empty string when no document
lookup is needed (a pure greeting, or a question only about the user's own application or documents).

searchQuery examples:
"ايه التراكات المتاحة في الإسكندرية" -> "tracks offered at Alexandria branch"
"ما هي مواعيد التقديم؟" -> "application dates and deadlines"
"What about its prerequisites?" (after discussing the .NET track) -> ".NET track prerequisites"
"مرحبا" -> ""
"هل تم قبولي؟" -> ""

Examples:
"Which are available tracks?" -> knowledge
"What tracks does ITI offer?" -> knowledge
"ما هي المسارات المتاحة؟" -> knowledge
"Tell me about the .NET track" -> knowledge
"What is the minimum graduation grade?" -> knowledge
"How do I apply?" -> knowledge
"Hi" -> knowledge
"مرحبا" -> knowledge
"Thank you!" -> knowledge
"How are you?" -> knowledge
"Which track should I choose?" -> recommendation
"Which track fits someone with a design background?" -> recommendation
"ما هو المسار المناسب لي؟" -> recommendation
"What is my application status?" -> application_status
"هل تم قبولي؟" -> application_status
"Did you receive my documents?" -> document_status
"Was my transcript approved?" -> document_status
"هل تم قبول مستنداتي؟" -> document_status
"Which documents do I still need?" -> document_status

Multi-intent examples:
"What is my application status, and which track should I choose?" -> application_status, recommendation
"ما هي المسارات المتاحة وهل تم قبولي؟" -> knowledge, application_status
"Tell me the admission requirements and whether my documents arrived" -> knowledge, document_status
""";

    private readonly IGeminiChatClient _chatClient;
    private readonly IntentClassificationOptions _options;
    private readonly ILogger<IntentClassifierService> _logger;

    public IntentClassifierService(
        IGeminiChatClient chatClient,
        IOptions<IntentClassificationOptions> options,
        ILogger<IntentClassifierService> logger)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _options = options?.Value ?? new IntentClassificationOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IntentClassificationDto> ClassifyAsync(
        string userQuestion,
        List<ConversationTurnDto> context,
        CancellationToken cancellationToken = default)
    {
        // Mask national IDs and other long digit runs before any user text reaches the model.
        var maskedQuestion = InputSanitizerService.MaskSensitiveNumbers(userQuestion);
        var contextText = context.Any()
            ? string.Join("\n", context.TakeLast(3).Select(t =>
                $"Q: {InputSanitizerService.MaskSensitiveNumbers(t.Question)}\nA: {InputSanitizerService.MaskSensitiveNumbers(t.Answer)}"))
            : "No prior conversation history.";

        var userMessage = $"""
Conversation so far:
{contextText}

Message to classify:
{maskedQuestion}
""";

        string? responseText;
        try
        {
            responseText = await _chatClient.GenerateJsonAsync(
                SystemPrompt, userMessage, ResponseSchema, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Quota or transport failure. Answering from the knowledge base beats failing the message.
            _logger.LogWarning(ex, "Intent classification call failed; falling back to 'knowledge'.");
            return KnowledgeFallback();
        }

        if (!TryParseIntentResponse(responseText, out var classification))
        {
            // Our problem, not the user's - answer anyway rather than forcing a clarification loop.
            _logger.LogWarning(
                "Intent classifier could not parse a valid intent; falling back to 'knowledge'. Raw response: {RawResponse}",
                responseText);
            return classification;
        }

        if (classification.Confidence < _options.MinConfidence)
        {
            _logger.LogInformation(
                "Intent '{Intent}' below confidence threshold ({Confidence} < {Threshold}); asking the user to clarify.",
                classification.PrimaryIntent, classification.Confidence, _options.MinConfidence);
            classification.RequiresClarification = true;
        }

        DropWeakSecondaryIntents(classification);

        return classification;
    }

    // Tolerates Markdown fences/stray prose and still accepts the older single-intent shape.
    internal static bool TryParseIntentResponse(string? responseText, out IntentClassificationDto result)
    {
        result = KnowledgeFallback();

        var json = ExtractJsonObject(responseText);
        if (json is null)
            return false;

        RawIntentResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<RawIntentResponse>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        if (parsed is null)
            return false;

        var candidates = parsed.Intents ?? new List<RawDetectedIntent>();

        // A model that ignored the array schema may still have answered the old way.
        if (candidates.Count == 0 && !string.IsNullOrWhiteSpace(parsed.PrimaryIntent))
            candidates.Add(new RawDetectedIntent { Intent = parsed.PrimaryIntent, Confidence = parsed.Confidence });

        var intents = candidates
            .Select(c => new DetectedIntentDto
            {
                Intent = (c.Intent ?? string.Empty).Trim().ToLowerInvariant(),
                Confidence = Math.Clamp(c.Confidence, 0m, 1m)
            })
            .Where(c => Array.IndexOf(ValidIntents, c.Intent) >= 0)
            // The same intent twice is meaningless; keep the most confident.
            .GroupBy(c => c.Intent)
            .Select(g => g.OrderByDescending(c => c.Confidence).First())
            .OrderByDescending(c => c.Confidence)
            .ToList();

        if (intents.Count == 0)
            return false;

        foreach (var intent in intents)
            intent.RoutedTo = RouteFor(intent.Intent);

        result = new IntentClassificationDto
        {
            Intents = intents,
            PrimaryIntent = intents[0].Intent,
            Confidence = intents[0].Confidence,
            RoutedTo = intents[0].RoutedTo,
            Language = NormalizeRouterLanguage(parsed.Language),
            SearchQuery = NormalizeSearchQuery(parsed.SearchQuery),
            RequiresClarification = false
        };

        return true;
    }

    // Only the two supported values are usable; anything else becomes null so the caller falls back.
    private static string? NormalizeRouterLanguage(string? language)
    {
        var normalized = language?.Trim().ToLowerInvariant();
        return normalized is "ar" or "en" ? normalized : null;
    }

    // Blank means no lookup is needed; an over-long value is dropped rather than embedded.
    private static string? NormalizeSearchQuery(string? searchQuery)
    {
        var normalized = searchQuery?.Trim();

        return string.IsNullOrEmpty(normalized) || normalized.Length > MaxSearchQueryLength
            ? null
            : normalized;
    }

    public string GetClarificationMessage(string language) => language == "ar"
        ? "لم أتأكد تماماً من سؤالك. هل تسأل عن معلومات عامة عن المعهد والمسارات، أم عن حالة طلبك، أم عن حالة مستنداتك، أم تريد ترشيح المسار المناسب لك؟"
        : "I'm not sure I understood. Are you asking about general information on ITI and its tracks, about your application status, about your document status, or for a recommendation on which track suits you?";

    // A barely-detected second intent costs a call and answers something unasked. The primary is always kept - clarification covers a weak one.
    private void DropWeakSecondaryIntents(IntentClassificationDto classification)
    {
        if (classification.Intents.Count < 2)
            return;

        var kept = classification.Intents
            .Take(1)
            .Concat(classification.Intents.Skip(1).Where(i => i.Confidence >= _options.MinConfidence))
            .ToList();

        if (kept.Count != classification.Intents.Count)
        {
            _logger.LogInformation(
                "Dropped {Count} secondary intent(s) below the confidence threshold of {Threshold}.",
                classification.Intents.Count - kept.Count, _options.MinConfidence);
            classification.Intents = kept;
        }
    }

    private static string RouteFor(string intent) => intent switch
    {
        "application_status" => "application_status_service",
        "document_status" => "document_status_service",
        "recommendation" => "recommendations_agent",
        _ => "knowledge_rag"
    };

    private static IntentClassificationDto KnowledgeFallback() => new()
    {
        Intents = new List<DetectedIntentDto>
        {
            new() { Intent = "knowledge", Confidence = 0.5m, RoutedTo = "knowledge_rag" }
        },
        PrimaryIntent = "knowledge",
        Confidence = 0.5m,
        RoutedTo = "knowledge_rag",
        RequiresClarification = false
    };

    private sealed class RawIntentResponse
    {
        public List<RawDetectedIntent>? Intents { get; set; }

        // Only present when a model answered in the pre-multi-intent shape.
        public string? PrimaryIntent { get; set; }

        public decimal Confidence { get; set; }

        // The language the model detected the message is written in ("ar" / "en").
        public string? Language { get; set; }

        // A self-contained English query for the knowledge lookup ("" when none is needed).
        public string? SearchQuery { get; set; }
    }

    private sealed class RawDetectedIntent
    {
        public string? Intent { get; set; }

        public decimal Confidence { get; set; }
    }

    private static string? ExtractJsonObject(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var t = text.Trim();

        if (t.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = t.IndexOf('\n');
            if (firstNewline >= 0)
                t = t.Substring(firstNewline + 1);

            var closingFence = t.LastIndexOf("```", StringComparison.Ordinal);
            if (closingFence >= 0)
                t = t.Substring(0, closingFence);

            t = t.Trim();
        }

        var start = t.IndexOf('{');
        var end = t.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;

        return t.Substring(start, end - start + 1);
    }
}
