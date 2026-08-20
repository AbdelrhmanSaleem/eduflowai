using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EduFlowAI.AI.Application.DocumentVerification;

public sealed record DocumentVerificationContext(
    Guid ApplicationId,
    string DocumentType,
    IReadOnlyDictionary<string, string> ExpectedFields);

public interface IDocumentVerificationContextReader
{
    Task<DocumentVerificationContext> GetAsync(
        Guid applicationId,
        string documentType,
        CancellationToken cancellationToken);
}
