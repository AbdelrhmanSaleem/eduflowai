using System.Threading;
using System.Threading.Tasks;

namespace EduFlowAI.AI.Application.Interfaces;

public interface IDocumentTextExtractor
{
    Task<string> ExtractTextAsync(byte[] content, string fileName, CancellationToken cancellationToken = default);
}
