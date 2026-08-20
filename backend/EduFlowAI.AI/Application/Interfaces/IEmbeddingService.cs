using System.Threading;
using System.Threading.Tasks;

namespace EduFlowAI.AI.Application.Interfaces;

public interface IEmbeddingService
{
    // Width comes from Gemini:EmbeddingDimensions and must match the vector column.
   
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}