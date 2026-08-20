using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DocumentVerification;
using EduFlowAI.Documents.Application.Interfaces;

namespace EduFlowAI.AI.Infrastructure.DocumentVerification;

// EduFlowAI.AI already references EduFlowAI.Documents, so this adapter lives here
// rather than being implemented on the Documents side.
public sealed class DocumentVerificationFileReader : IDocumentVerificationFileReader
{
    private readonly IFileStorageService _fileStorageService;

    public DocumentVerificationFileReader(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
        => _fileStorageService.OpenReadAsync(storageKey, cancellationToken);
}
