using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DocumentVerification;
using EduFlowAI.AI.Infrastructure.DocumentVerification.Gemini;
using EduFlowAI.Shared.Messaging.Contracts.Documents.V1;

namespace EduFlowAI.AI.Infrastructure.DocumentVerification;

public sealed class DocumentVerificationService : IDocumentVerificationService
{
    // "Retry malformed/failed verification once" - one extra attempt on top of the first,
    // and only for a structurally invalid response, not for transient/network failures
    // (those propagate out of the Gemini client untouched, for Wolverine to retry the message).
    private const int MaxStructuredAttempts = 2;

    private static readonly HashSet<string> SensitiveFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "NationalId"
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IDocumentVerificationGeminiClient _geminiClient;

    public DocumentVerificationService(IDocumentVerificationGeminiClient geminiClient)
    {
        _geminiClient = geminiClient;
    }

    public async Task<DocumentVerificationResult> VerifyAsync(
        DocumentVerificationInput input,
        CancellationToken cancellationToken)
    {
        var fileContent = await ReadAllBytesAsync(input.File, cancellationToken);
        if (fileContent.Length == 0)
        {
            throw new DocumentVerificationFinalException(
                "DOCUMENT_UNSUPPORTED_FORMAT",
                "The submitted document is empty.",
                attemptCount: 1);
        }

        var mimeType = ResolveMimeType(input.OriginalFileName);
        if (mimeType is null)
        {
            throw new DocumentVerificationFinalException(
                "DOCUMENT_UNSUPPORTED_FORMAT",
                "Only PDF, JPG, JPEG and PNG documents are supported for verification.",
                attemptCount: 1);
        }

        GeminiVerificationResponseDto? parsed = null;
        Exception? lastError = null;
        var attempts = 0;

        for (attempts = 1; attempts <= MaxStructuredAttempts; attempts++)
        {
            // Transient/provider exceptions (HttpRequestException, timeouts, ...) are not caught
            // here - they propagate out of VerifyAsync, out of the handler, and let Wolverine's
            // retry/DLQ policy handle the whole message.
            var rawJson = await _geminiClient.GenerateVerificationJsonAsync(
                input.DocumentType,
                input.ExpectedFields,
                fileContent,
                mimeType,
                cancellationToken);

            if (TryParseAndValidate(rawJson, input.ExpectedFields, out parsed, out var validationError))
            {
                break;
            }

            lastError = validationError;
            parsed = null;
        }

        if (parsed is null)
        {
            throw new DocumentVerificationFinalException(
                "DOCUMENT_RESPONSE_INVALID",
                "The verification provider did not return a usable structured result.",
                attemptCount: attempts - 1,
                innerException: lastError);
        }

        return new DocumentVerificationResult(parsed.Outcome, BuildDetails(parsed, input.ExpectedFields));
    }

    private bool TryParseAndValidate(
        string rawJson,
        IReadOnlyDictionary<string, string> expectedFields,
        out GeminiVerificationResponseDto? result,
        out Exception? error)
    {
        result = null;
        error = null;

        GeminiVerificationResponseRawDto? raw;
        try
        {
            raw = JsonSerializer.Deserialize<GeminiVerificationResponseRawDto>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            error = ex;
            return false;
        }

        if (raw is null)
        {
            error = new JsonException("Gemini returned a null verification response.");
            return false;
        }

        if (!Enum.TryParse<DocumentVerificationOutcomeV1>(raw.Outcome, ignoreCase: true, out var outcome))
        {
            error = new JsonException($"Gemini returned an unknown outcome '{raw.Outcome}'.");
            return false;
        }

        var fields = raw.Fields ?? new List<GeminiVerificationFieldRawDto>();

        // Exactly one result per expected field: no missing, no duplicate, no extra.
        var fieldNamesReturned = fields
            .Select(f => f.FieldName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList();

        var hasDuplicates = fieldNamesReturned.Count != fieldNamesReturned.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var expectedNames = new HashSet<string>(expectedFields.Keys, StringComparer.OrdinalIgnoreCase);
        var returnedNames = new HashSet<string>(fieldNamesReturned, StringComparer.OrdinalIgnoreCase);

        var missingFromResponse = expectedNames.Except(returnedNames, StringComparer.OrdinalIgnoreCase).Any();
        var extraInResponse = returnedNames.Except(expectedNames, StringComparer.OrdinalIgnoreCase).Any();

        if (hasDuplicates || missingFromResponse || extraInResponse || fields.Any(f => string.IsNullOrWhiteSpace(f.FieldName)))
        {
            error = new JsonException(
                "Gemini did not return exactly one result per expected field (missing, duplicate or extra field detected).");
            return false;
        }

        result = new GeminiVerificationResponseDto(
            outcome,
            fields,
            raw.Warnings ?? new List<string>(),
            raw.ConfidenceScore);

        return true;
    }

    private DocumentVerificationDetailsV1 BuildDetails(
        GeminiVerificationResponseDto response,
        IReadOnlyDictionary<string, string> expectedFields)
    {
        var comparisons = new List<VerificationFieldComparisonV1>();
        var missingFields = new List<string>();

        foreach (var field in response.Fields)
        {
            var fieldName = field.FieldName!;
            var isMatch = field.IsMatch ?? false;
            expectedFields.TryGetValue(fieldName, out var expectedValue);

            if (string.IsNullOrWhiteSpace(field.ExtractedValue))
            {
                missingFields.Add(fieldName);
            }

            comparisons.Add(new VerificationFieldComparisonV1(
                FieldName: fieldName,
                ExtractedValue: MaskIfSensitive(fieldName, field.ExtractedValue),
                ExpectedValue: MaskIfSensitive(fieldName, expectedValue),
                IsMatch: isMatch,
                Notes: field.Notes));
        }

        return new DocumentVerificationDetailsV1(
            Fields: comparisons,
            MissingFields: missingFields,
            Warnings: response.Warnings,
            ConfidenceScore: response.ConfidenceScore is null ? null : (decimal)response.ConfidenceScore.Value,
            ModelName: "gemini-document-verification");
    }

    private static string? MaskIfSensitive(string fieldName, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (!SensitiveFieldNames.Contains(fieldName))
        {
            return value;
        }

        if (value.Length <= 4)
        {
            return new string('*', value.Length);
        }

        var visibleStart = value[..2];
        var visibleEnd = value[^2..];
        var maskedMiddle = new string('*', value.Length - 4);
        return $"{visibleStart}{maskedMiddle}{visibleEnd}";
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }

    private static string? ResolveMimeType(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => null
        };
    }

    private sealed record GeminiVerificationResponseDto(
        DocumentVerificationOutcomeV1 Outcome,
        List<GeminiVerificationFieldRawDto> Fields,
        List<string> Warnings,
        double? ConfidenceScore);

    private sealed class GeminiVerificationResponseRawDto
    {
        public string? Outcome { get; set; }
        public List<GeminiVerificationFieldRawDto>? Fields { get; set; }
        public List<string>? Warnings { get; set; }
        public double? ConfidenceScore { get; set; }
    }

    private sealed class GeminiVerificationFieldRawDto
    {
        public string? FieldName { get; set; }
        public string? ExtractedValue { get; set; }
        public bool? IsMatch { get; set; }
        public string? Notes { get; set; }
    }
}
