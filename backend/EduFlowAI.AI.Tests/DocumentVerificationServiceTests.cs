using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DocumentVerification;
using EduFlowAI.AI.Infrastructure.DocumentVerification;
using EduFlowAI.AI.Infrastructure.DocumentVerification.Gemini;
using EduFlowAI.Shared.Messaging.Contracts.Documents.V1;

namespace EduFlowAI.AI.Tests;

public class DocumentVerificationServiceTests
{
    private static DocumentVerificationInput BuildInput(
        Stream file,
        string documentType,
        string originalFileName,
        IReadOnlyDictionary<string, string> expectedFields) =>
        new(
            DocumentId: Guid.NewGuid(),
            DocumentType: documentType,
            OriginalFileName: originalFileName,
            File: file,
            ExpectedFields: expectedFields);

    [Fact]
    public async Task VerifyAsync_ExactMatch_MasksNationalIdAndReturnsUnmaskedNonSensitiveFields()
    {
        var expectedFields = new Dictionary<string, string>
        {
            ["NationalId"] = "29001011234567",
            ["FullNameAr"] = "Ahmed Mohamed"
        };

        const string responseJson = """
            {
              "outcome": "ExactMatch",
              "fields": [
                { "fieldName": "NationalId", "extractedValue": "29001011234567", "isMatch": true },
                { "fieldName": "FullNameAr", "extractedValue": "Ahmed Mohamed", "isMatch": true }
              ],
              "warnings": [],
              "confidenceScore": 0.95
            }
            """;

        var client = new FakeGeminiClient(responseJson);
        var service = new DocumentVerificationService(client);

        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var result = await service.VerifyAsync(
            BuildInput(stream, "NationalId", "id.pdf", expectedFields),
            CancellationToken.None);

        Assert.Equal(DocumentVerificationOutcomeV1.ExactMatch, result.Outcome);
        Assert.Equal(1, client.CallCount);
        Assert.Empty(result.Details.MissingFields);

        var nationalId = result.Details.Fields.Single(f => f.FieldName == "NationalId");
        Assert.DoesNotContain("29001011234567", nationalId.ExtractedValue);
        Assert.Contains('*', nationalId.ExtractedValue!.ToCharArray());

        var fullName = result.Details.Fields.Single(f => f.FieldName == "FullNameAr");
        Assert.Equal("Ahmed Mohamed", fullName.ExtractedValue);
    }

    [Fact]
    public async Task VerifyAsync_MissingExpectedField_RetriesOnceThenThrowsFinal()
    {
        var expectedFields = new Dictionary<string, string> { ["NationalId"] = "123", ["FullNameAr"] = "X" };

        const string missingFieldJson = """
            { "outcome": "ExactMatch", "fields": [ { "fieldName": "NationalId", "extractedValue": "123", "isMatch": true } ] }
            """;

        var client = new FakeGeminiClient(missingFieldJson, missingFieldJson);
        var service = new DocumentVerificationService(client);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var ex = await Assert.ThrowsAsync<DocumentVerificationFinalException>(() =>
            service.VerifyAsync(BuildInput(stream, "NationalId", "id.pdf", expectedFields), CancellationToken.None));

        Assert.Equal("DOCUMENT_RESPONSE_INVALID", ex.ErrorCode);
        Assert.Equal(2, client.CallCount);
        Assert.Equal(2, ex.AttemptCount);
    }

    [Fact]
    public async Task VerifyAsync_DuplicateField_TreatedAsInvalid()
    {
        var expectedFields = new Dictionary<string, string> { ["NationalId"] = "123" };

        const string duplicateFieldJson = """
            {
              "outcome": "ExactMatch",
              "fields": [
                { "fieldName": "NationalId", "extractedValue": "123", "isMatch": true },
                { "fieldName": "NationalId", "extractedValue": "123", "isMatch": true }
              ]
            }
            """;

        var client = new FakeGeminiClient(duplicateFieldJson, duplicateFieldJson);
        var service = new DocumentVerificationService(client);

        using var stream = new MemoryStream(new byte[] { 1 });
        var ex = await Assert.ThrowsAsync<DocumentVerificationFinalException>(() =>
            service.VerifyAsync(BuildInput(stream, "NationalId", "id.pdf", expectedFields), CancellationToken.None));

        Assert.Equal("DOCUMENT_RESPONSE_INVALID", ex.ErrorCode);
    }

    [Fact]
    public async Task VerifyAsync_ExtraUnexpectedField_TreatedAsInvalid()
    {
        var expectedFields = new Dictionary<string, string> { ["NationalId"] = "123" };

        const string extraFieldJson = """
            {
              "outcome": "ExactMatch",
              "fields": [
                { "fieldName": "NationalId", "extractedValue": "123", "isMatch": true },
                { "fieldName": "SomeOtherField", "extractedValue": "x", "isMatch": true }
              ]
            }
            """;

        var client = new FakeGeminiClient(extraFieldJson, extraFieldJson);
        var service = new DocumentVerificationService(client);

        using var stream = new MemoryStream(new byte[] { 1 });
        var ex = await Assert.ThrowsAsync<DocumentVerificationFinalException>(() =>
            service.VerifyAsync(BuildInput(stream, "NationalId", "id.pdf", expectedFields), CancellationToken.None));

        Assert.Equal("DOCUMENT_RESPONSE_INVALID", ex.ErrorCode);
    }

    [Fact]
    public async Task VerifyAsync_MalformedJsonOnFirstAttempt_ValidOnSecond_Succeeds()
    {
        var expectedFields = new Dictionary<string, string> { ["NationalId"] = "123" };

        const string malformed = "this is not json";
        const string valid = """
            { "outcome": "ExactMatch", "fields": [ { "fieldName": "NationalId", "extractedValue": "123", "isMatch": true } ] }
            """;

        var client = new FakeGeminiClient(malformed, valid);
        var service = new DocumentVerificationService(client);

        using var stream = new MemoryStream(new byte[] { 1 });
        var result = await service.VerifyAsync(
            BuildInput(stream, "NationalId", "id.pdf", expectedFields),
            CancellationToken.None);

        Assert.Equal(DocumentVerificationOutcomeV1.ExactMatch, result.Outcome);
        Assert.Equal(2, client.CallCount);
    }

    [Fact]
    public async Task VerifyAsync_UnsupportedExtension_FailsWithoutCallingGemini()
    {
        var client = new FakeGeminiClient();
        var service = new DocumentVerificationService(client);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var ex = await Assert.ThrowsAsync<DocumentVerificationFinalException>(() =>
            service.VerifyAsync(
                BuildInput(stream, "NationalId", "id.docx", new Dictionary<string, string> { ["NationalId"] = "1" }),
                CancellationToken.None));

        Assert.Equal("DOCUMENT_UNSUPPORTED_FORMAT", ex.ErrorCode);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task VerifyAsync_EmptyFile_FailsWithoutCallingGemini()
    {
        var client = new FakeGeminiClient();
        var service = new DocumentVerificationService(client);

        using var stream = new MemoryStream(Array.Empty<byte>());
        var ex = await Assert.ThrowsAsync<DocumentVerificationFinalException>(() =>
            service.VerifyAsync(
                BuildInput(stream, "NationalId", "id.pdf", new Dictionary<string, string> { ["NationalId"] = "1" }),
                CancellationToken.None));

        Assert.Equal("DOCUMENT_UNSUPPORTED_FORMAT", ex.ErrorCode);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task VerifyAsync_UnknownOutcomeValue_TreatedAsInvalid()
    {
        var expectedFields = new Dictionary<string, string> { ["NationalId"] = "123" };

        const string unknownOutcomeJson = """
            { "outcome": "TotallyMadeUp", "fields": [ { "fieldName": "NationalId", "extractedValue": "123", "isMatch": true } ] }
            """;

        var client = new FakeGeminiClient(unknownOutcomeJson, unknownOutcomeJson);
        var service = new DocumentVerificationService(client);

        using var stream = new MemoryStream(new byte[] { 1 });
        var ex = await Assert.ThrowsAsync<DocumentVerificationFinalException>(() =>
            service.VerifyAsync(BuildInput(stream, "NationalId", "id.pdf", expectedFields), CancellationToken.None));

        Assert.Equal("DOCUMENT_RESPONSE_INVALID", ex.ErrorCode);
    }

    private sealed class FakeGeminiClient : IDocumentVerificationGeminiClient
    {
        private readonly Queue<string> _responses;

        public int CallCount { get; private set; }

        public FakeGeminiClient(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        public Task<string> GenerateVerificationJsonAsync(
            string documentType,
            IReadOnlyDictionary<string, string> expectedFields,
            byte[] fileContent,
            string mimeType,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("Test did not expect another Gemini call.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
