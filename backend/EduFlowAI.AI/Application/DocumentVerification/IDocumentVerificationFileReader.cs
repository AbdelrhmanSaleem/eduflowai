using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EduFlowAI.AI.Application.DocumentVerification;

public interface IDocumentVerificationFileReader
{
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}
