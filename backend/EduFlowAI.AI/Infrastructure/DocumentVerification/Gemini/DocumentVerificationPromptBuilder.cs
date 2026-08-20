using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EduFlowAI.AI.Infrastructure.DocumentVerification.Gemini;

internal static class DocumentVerificationPromptBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string BuildInstruction(
        string documentType,
        IReadOnlyDictionary<string, string> expectedFields)
    {
        var expected = expectedFields.Select(pair => new { field = pair.Key, expectedValue = pair.Value });

        return $$"""
            You are a document verification assistant for a university admission platform.

            The attached file is claimed to be a "{{documentType}}" belonging to an applicant.

            Task:
            - Read the attached document and extract the value of each expected field listed below.
            - Compare each extracted value against the expected value supplied for that field.
            - Decide isMatch for each field: true only if the extracted value clearly matches the
              expected value (minor formatting differences, e.g. spacing or date format, still count
              as a match; a genuinely different value does not).

            Rules:
            - Return exactly one result per expected field below - no more, no fewer, no duplicates,
              no extra fields.
            - If the document cannot be read at all, set outcome to "UnreadableDocument".
            - If the document is clearly not a {{documentType}}, set outcome to "InvalidDocumentType".
            - If one or more expected fields cannot be found in the document, set outcome to
              "MissingRequiredData" and still return a result row for every expected field (leave
              extractedValue null and isMatch false for the ones you could not find).
            - If every expected field was found and every one matches, set outcome to "ExactMatch".
            - If every expected field was found but at least one does not match, set outcome to
              "ValidButDifferent".
            - Never guess or invent a value. Only report what is actually visible in the document.
            - Return JSON matching the requested schema exactly. Do not include any commentary.

            Expected fields:
            {{JsonSerializer.Serialize(expected, JsonOptions)}}
            """;
    }

    public static object BuildResponseSchema() => new
    {
        type = "object",
        properties = new
        {
            outcome = new
            {
                type = "string",
                @enum = new[]
                {
                    "ExactMatch",
                    "ValidButDifferent",
                    "MissingRequiredData",
                    "UnreadableDocument",
                    "InvalidDocumentType"
                }
            },
            fields = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        fieldName = new { type = "string" },
                        extractedValue = new { type = "string", nullable = true },
                        isMatch = new { type = "boolean" },
                        notes = new { type = "string", nullable = true }
                    },
                    required = new[] { "fieldName", "isMatch" }
                }
            },
            warnings = new
            {
                type = "array",
                items = new { type = "string" }
            },
            confidenceScore = new
            {
                type = "number",
                nullable = true
            }
        },
        required = new[] { "outcome", "fields" }
    };
}
