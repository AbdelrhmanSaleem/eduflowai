using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EduFlowAI.AI.Infrastructure.DocumentVerification.Gemini;

// One raw call to Gemini for one document. Retry-once semantics on a malformed/failed
// structured response live in DocumentVerificationService, not here - this client makes a
// single attempt and either returns the raw JSON text or throws.
public interface IDocumentVerificationGeminiClient
{
    Task<string> GenerateVerificationJsonAsync(
        string documentType,
        IReadOnlyDictionary<string, string> expectedFields,
        byte[] fileContent,
        string mimeType,
        CancellationToken cancellationToken);
}
