using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EduFlowAI.AI.Application.Interfaces;

// Callers work with opaque relative keys such as "knowledge-base/{id}.pdf".
public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string relativeKey, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string relativeKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativeKey, CancellationToken cancellationToken = default);

    bool Exists(string relativeKey);
}
