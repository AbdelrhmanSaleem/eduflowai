using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Documents.Domain.Enums;

namespace EduFlowAI.AI.Application.Services;

// Hybrid document-status answer: the statuses are deterministic facts (never decided by the model);
// the model phrases them warmly, in ITI's voice, for both the "has documents" and "no documents yet"
// cases - so the reply is never a canned template. Falls back to the plain facts only if the model
// returns nothing or is unavailable, so a status question always gets a truthful answer.
public sealed class DocumentStatusNarrator : IDocumentStatusNarrator
{
    private readonly IGeminiChatClient _chatClient;

    public DocumentStatusNarrator(IGeminiChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    public async Task<string> NarrateAsync(
        IEnumerable<ApplicantDocumentDto> documents,
        string language,
        bool isContinuingConversation = false,
        CancellationToken cancellationToken = default)
    {
        var docs = documents?.ToList() ?? new List<ApplicantDocumentDto>();

        // Deterministic, authoritative facts. English is canonical; the model phrases and translates,
        // it never re-decides. The empty case is a fact too, so it is phrased naturally as well.
        var facts = docs.Count == 0
            ? "The applicant has not uploaded any documents yet."
            : string.Join("\n", docs
                .OrderBy(d => d.DocumentType)
                .Select(d => $"- {TypeLabel(d.DocumentType)}: {StatusLabel(d.Status)}"));

        var languageName = language == "ar" ? "Arabic" : "English";

        // Without this the narrator opens cold, so switching intent produced a fresh welcome.
        var greetingRule = isContinuingConversation
            ? "- You are mid-conversation: do NOT greet or introduce the institute again. Answer directly."
            : "- A brief warm opening is fine before the answer.";

        var systemPrompt = $"""
# ROLE
You are the official virtual assistant of the Information Technology Institute (ITI), replying to an
applicant who asked about their uploaded documents.

# LANGUAGE
Write your ENTIRE reply in {languageName}. The facts below are in English, but you MUST answer only in
{languageName}, translating the document names and statuses. Never switch languages mid-answer.

# THE FACTS (authoritative - this is the applicant's real record)
{facts}

# RULES
- Speak as the institute, first person plural ("we"), in a warm, human, professional tone. Never sound
  like a template or a form; vary your wording naturally.
{greetingRule}
- Report exactly these facts. Never add, change, soften, upgrade or downgrade a status, and never
  invent documents, dates, timelines or next steps.
- If nothing has been uploaded yet, say so kindly and mention they can upload from their account.
- If something is rejected or still under review, say so plainly but reassuringly.
- Keep it to one short, natural paragraph (a short list only if it genuinely reads better). No filler,
  no emoji.
- Write plain text only. Never use Markdown: no asterisks, underscores, backticks or hash symbols. If
  you list items, number them: each on its own line starting with "1. ", "2. ", and so on.
""";

        string? answer;
        try
        {
            answer = await _chatClient.GenerateAsync(
                systemPrompt, "Tell me the status of my documents.", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            answer = null; // model unavailable (e.g. quota) - fall back to the plain facts below
        }

        return string.IsNullOrWhiteSpace(answer) ? Fallback(docs, language) : answer;
    }

    // Used only when the model is unavailable, so the applicant still gets a truthful answer.
    private static string Fallback(IReadOnlyList<ApplicantDocumentDto> docs, string language)
    {
        if (docs.Count == 0)
            return language == "ar"
                ? "لا توجد مستندات مرفوعة على حسابك حتى الآن. يمكنك رفعها من حسابك في أي وقت."
                : "You haven't uploaded any documents yet. You can upload them from your account whenever you're ready.";

        var intro = language == "ar" ? "حالة مستنداتك:" : "Your document status:";
        var lines = docs
            .OrderBy(d => d.DocumentType)
            .Select(d => $"• {TypeLabel(d.DocumentType)}: {StatusLabel(d.Status)}");
        return intro + "\n" + string.Join("\n", lines);
    }

    private static string TypeLabel(DocumentType type) => type switch
    {
        DocumentType.NationalId => "National ID",
        DocumentType.BirthCertificate => "Birth Certificate",
        DocumentType.GraduationCertificate => "Graduation Certificate",
        DocumentType.MilitaryCertificate => "Military Certificate",
        _ => "Document"
    };

    private static string StatusLabel(DocumentStatus status) => status switch
    {
        DocumentStatus.Uploaded => "Received (awaiting review)",
        DocumentStatus.Verifying => "Under verification",
        DocumentStatus.Approved => "Approved",
        DocumentStatus.NeedsHumanReview => "Under manual review",
        DocumentStatus.Rejected => "Rejected (needs re-upload)",
        _ => "Pending"
    };
}
