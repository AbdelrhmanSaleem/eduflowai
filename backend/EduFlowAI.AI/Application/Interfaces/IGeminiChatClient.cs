using System.Threading;
using System.Threading.Tasks;

namespace EduFlowAI.AI.Application.Interfaces;

public interface IGeminiChatClient
{
    Task<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);

    // Forces the response to match the schema, at temperature 0.
    Task<string> GenerateJsonAsync(string systemPrompt, string userMessage, object responseSchema, CancellationToken cancellationToken = default);

    // Sends a document as inline data for transcription.
    Task<string> GenerateFromDocumentAsync(string instruction, byte[] document, string mimeType, CancellationToken cancellationToken = default);
}
