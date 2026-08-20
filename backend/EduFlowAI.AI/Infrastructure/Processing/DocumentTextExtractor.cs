using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Infrastructure.ExternalServices;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Infrastructure.Processing;

public class DocumentTextExtractor : IDocumentTextExtractor
{
    private const string TranscriptionInstruction = """
Transcribe this document into clean, faithful Markdown for a knowledge base.

Rules:
- Preserve all factual content: headings, lists, tables, numbers, dates, names and links.
- Follow the correct reading order; for multi-column layouts read each column in logical order.
- Keep tabular data as Markdown tables.
- Do not summarise, interpret, add commentary, or invent anything not present in the document.
- Output only the transcription.
""";

    private readonly IGeminiChatClient _chatClient;
    private readonly PdfProcessingService _pdfProcessor;
    private readonly IngestionOptions _options;
    private readonly ILogger<DocumentTextExtractor> _logger;

    public DocumentTextExtractor(
        IGeminiChatClient chatClient,
        PdfProcessingService pdfProcessor,
        IOptions<IngestionOptions> options,
        ILogger<DocumentTextExtractor> logger)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _pdfProcessor = pdfProcessor ?? throw new ArgumentNullException(nameof(pdfProcessor));
        _options = options?.Value ?? new IngestionOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ExtractTextAsync(byte[] content, string fileName, CancellationToken cancellationToken = default)
    {
        if (content == null || content.Length == 0)
            throw new ArgumentException("Document content cannot be empty.", nameof(content));

        if (IsPlainText(fileName))
            return DecodeText(content);

        // iText loses ligatures and interleaves multi-column layouts, so prefer model transcription with local extraction as fallback.
        if (_options.UseAiExtraction && content.Length <= _options.MaxInlineBytesForAi)
        {
            try
            {
                var transcript = await _chatClient.GenerateFromDocumentAsync(
                    TranscriptionInstruction, content, "application/pdf", cancellationToken);

                if (!string.IsNullOrWhiteSpace(transcript))
                    return transcript;

                _logger.LogWarning("AI transcription returned empty text for {FileName}; using local extraction.", fileName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "AI transcription failed for {FileName}; using local extraction.", fileName);
            }
        }
        else if (content.Length > _options.MaxInlineBytesForAi)
        {
            _logger.LogInformation(
                "{FileName} is {Bytes} bytes, above the inline limit; using local extraction.", fileName, content.Length);
        }

        using var stream = new MemoryStream(content, writable: false);
        return _pdfProcessor.ExtractRawText(stream);
    }

    public static bool IsPlainText(string fileName) =>
        fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
        fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
        fileName.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);

    private static string DecodeText(byte[] content)
    {
        using var reader = new StreamReader(new MemoryStream(content, writable: false), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
