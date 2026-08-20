using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.Shared.Messaging.Contracts.Documents.V1;

namespace EduFlowAI.AI.Application.DocumentVerification;

public sealed record DocumentVerificationInput(
    Guid DocumentId,
    string DocumentType,
    string OriginalFileName,
    Stream File,
    IReadOnlyDictionary<string, string> ExpectedFields);

public sealed record DocumentVerificationResult(
    DocumentVerificationOutcomeV1 Outcome,
    DocumentVerificationDetailsV1 Details);

public interface IDocumentVerificationService
{
    Task<DocumentVerificationResult> VerifyAsync(
        DocumentVerificationInput input,
        CancellationToken cancellationToken);
}
