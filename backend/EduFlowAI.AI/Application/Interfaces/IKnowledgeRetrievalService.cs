using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DTOs;

namespace EduFlowAI.AI.Application.Interfaces;

public interface IKnowledgeRetrievalService
{
    // Nearest chunks by cosine distance, each carrying the document it came from.
    Task<List<RetrievedChunkDto>> RetrieveContextAsync(
        string query,
        int limit = 5,
        CancellationToken cancellationToken = default);
}
