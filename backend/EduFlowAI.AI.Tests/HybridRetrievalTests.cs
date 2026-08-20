using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Application.Services;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Tests;

// A dense vector ranked the Alexandria chunk out of the top results; the keyword pass rescues it.
public class HybridRetrievalTests
{
    [Fact]
    public async Task Keyword_hits_are_kept_even_when_vector_hits_fill_the_budget()
    {
        var repository = new StubRepository
        {
            VectorHits = Chunks("v", 10),
            KeywordHits = Chunks("alexandria", 2)
        };
        var service = Build(repository, maxContextChunks: 10, maxKeywordChunks: 4);

        var results = await service.RetrieveContextAsync("tracks offered at Alexandria branch", 10);

        Assert.Equal(10, results.Count);
        Assert.Contains(results, r => r.SourceTitle == "alexandria-0");
        Assert.Contains(results, r => r.SourceTitle == "alexandria-1");
    }

    [Fact]
    public async Task The_same_chunk_from_both_passes_appears_once()
    {
        var shared = new RetrievedChunkDto { ChunkId = Guid.NewGuid(), Content = "shared", SourceTitle = "shared" };
        var repository = new StubRepository
        {
            VectorHits = new List<RetrievedChunkDto> { shared },
            KeywordHits = new List<RetrievedChunkDto> { shared }
        };
        var service = Build(repository, maxContextChunks: 5, maxKeywordChunks: 2);

        var results = await service.RetrieveContextAsync("Alexandria branch tracks", 5);

        Assert.Single(results);
    }

    [Fact]
    public async Task A_query_with_no_meaningful_terms_skips_the_keyword_pass()
    {
        var repository = new StubRepository { VectorHits = Chunks("v", 2), KeywordHits = Chunks("k", 2) };
        var service = Build(repository, maxContextChunks: 5, maxKeywordChunks: 2);

        // Every word here is either too short or a stopword, so there is nothing worth matching literally.
        var results = await service.RetrieveContextAsync("what are the most", 5);

        Assert.Null(repository.LastKeywordTerms);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task An_empty_query_retrieves_nothing()
    {
        var service = Build(new StubRepository(), maxContextChunks: 5, maxKeywordChunks: 2);

        Assert.Empty(await service.RetrieveContextAsync("   ", 5));
    }

    private static KnowledgeBaseRetrievalService Build(
        StubRepository repository,
        int maxContextChunks,
        int maxKeywordChunks)
    {
        var options = Options.Create(new RetrievalOptions
        {
            MaxContextChunks = maxContextChunks,
            MaxKeywordChunks = maxKeywordChunks
        });

        return new KnowledgeBaseRetrievalService(new StubEmbedding(), repository, options);
    }

    private static List<RetrievedChunkDto> Chunks(string prefix, int count) =>
        Enumerable.Range(0, count)
            .Select(i => new RetrievedChunkDto
            {
                ChunkId = Guid.NewGuid(),
                Content = $"{prefix} content {i}",
                SourceTitle = $"{prefix}-{i}"
            })
            .ToList();

    private sealed class StubEmbedding : IEmbeddingService
    {
        public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
            => Task.FromResult(new float[] { 0.1f, 0.2f });
    }

    private sealed class StubRepository : IKnowledgeRepository
    {
        public List<RetrievedChunkDto> VectorHits { get; set; } = new();
        public List<RetrievedChunkDto> KeywordHits { get; set; } = new();
        public IReadOnlyList<string>? LastKeywordTerms { get; private set; }

        public Task<List<RetrievedChunkDto>> SearchChunksByEmbeddingAsync(
            Pgvector.Vector embedding, int limit = 5, CancellationToken cancellationToken = default)
            => Task.FromResult(VectorHits.Take(limit).ToList());

        public Task<List<RetrievedChunkDto>> SearchChunksByKeywordAsync(
            IReadOnlyList<string> terms, int limit, CancellationToken cancellationToken = default)
        {
            LastKeywordTerms = terms;
            return Task.FromResult(KeywordHits.Take(limit).ToList());
        }

        public Task<EduFlowAI.AI.Domain.Entities.KnowledgeBaseDocument?> GetDocumentByIdAsync(
            Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<EduFlowAI.AI.Domain.Entities.KnowledgeBaseDocument>> GetAllDocumentsAsync(
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddDocumentAsync(
            EduFlowAI.AI.Domain.Entities.KnowledgeBaseDocument document,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateDocumentAsync(
            EduFlowAI.AI.Domain.Entities.KnowledgeBaseDocument document,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteDocumentAsync(
            EduFlowAI.AI.Domain.Entities.KnowledgeBaseDocument document,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddChunksAsync(
            List<EduFlowAI.AI.Domain.Entities.KnowledgeBaseChunk> chunks,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetChunkCountAsync(Guid documentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task DeleteAllChunksAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
