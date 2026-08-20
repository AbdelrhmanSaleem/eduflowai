using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Application.Services;

// Hybrid retrieval: embedding neighbours plus a literal keyword pass, so an exact name is never missed.
public class KnowledgeBaseRetrievalService : IKnowledgeRetrievalService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IKnowledgeRepository _repository;
    private readonly RetrievalOptions _options;

    public KnowledgeBaseRetrievalService(
        IEmbeddingService embeddingService,
        IKnowledgeRepository repository,
        IOptions<RetrievalOptions>? options = null)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _options = options?.Value ?? new RetrievalOptions();
    }

    public async Task<List<RetrievedChunkDto>> RetrieveContextAsync(
        string query,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<RetrievedChunkDto>();

        var queryVectorArray = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        var queryVector = new Pgvector.Vector(queryVectorArray);

        var vectorHits = await _repository.SearchChunksByEmbeddingAsync(
            queryVector, limit, cancellationToken);

        var terms = SearchTermExtractor.Extract(query);
        if (terms.Count == 0)
            return vectorHits;

        var keywordBudget = Math.Min(_options.MaxKeywordChunks, limit);
        if (keywordBudget <= 0)
            return vectorHits;

        var keywordHits = await _repository.SearchChunksByKeywordAsync(
            terms, keywordBudget, cancellationToken);

        return Merge(vectorHits, keywordHits, limit, keywordBudget);
    }

    // Slots are reserved for literal matches, or the nearest neighbours crowd them out.
    private static List<RetrievedChunkDto> Merge(
        List<RetrievedChunkDto> vectorHits,
        List<RetrievedChunkDto> keywordHits,
        int limit,
        int reservedForKeywords)
    {
        var merged = new List<RetrievedChunkDto>();
        var seen = new HashSet<Guid>();

        void TryAdd(RetrievedChunkDto hit)
        {
            if (merged.Count < limit && seen.Add(hit.ChunkId))
                merged.Add(hit);
        }

        foreach (var hit in vectorHits.Take(Math.Max(0, limit - reservedForKeywords)))
            TryAdd(hit);

        foreach (var hit in keywordHits)
            TryAdd(hit);

        foreach (var hit in vectorHits)
            TryAdd(hit);

        return merged;
    }
}
