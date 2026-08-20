using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Domain.Entities;

namespace EduFlowAI.AI.Tests;

internal sealed class FakeTextExtractor : IDocumentTextExtractor
{
    public Exception? ThrowOnExtract { get; set; }

    // Lets one document in a batch fail while the rest succeed.
    public string? FailForFileName { get; set; }

    public Task<string> ExtractTextAsync(byte[] content, string fileName, CancellationToken cancellationToken = default)
    {
        if (ThrowOnExtract != null && (FailForFileName == null || FailForFileName == fileName))
            throw ThrowOnExtract;

        return Task.FromResult("The .NET Enterprise Solutions track runs for nine months at ITI.");
    }
}

internal sealed class FakeEmbeddingService : IEmbeddingService
{
    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult(new float[] { text.Length, 0f, 1f });
}

internal sealed class FakeFileStorage : IFileStorageService
{
    public Dictionary<string, byte[]> Files { get; } = new();

    public async Task<string> SaveAsync(Stream content, string relativeKey, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        Files[relativeKey] = buffer.ToArray();
        return relativeKey;
    }

    public Task<Stream> OpenReadAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        if (!Files.TryGetValue(relativeKey, out var bytes))
            throw new FileNotFoundException(relativeKey);

        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        Files.Remove(relativeKey);
        return Task.CompletedTask;
    }

    public bool Exists(string relativeKey) => Files.ContainsKey(relativeKey);
}

// Records what was queued. Unlike the real queue this one ends once drained, so a worker
// under test finishes instead of waiting forever for more work.
internal sealed class FakeIndexingQueue : IIndexingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public List<Guid> Enqueued { get; } = new();

    public ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        Enqueued.Add(documentId);
        return _channel.Writer.WriteAsync(documentId, cancellationToken);
    }

    public async IAsyncEnumerable<Guid> DequeueAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        while (_channel.Reader.TryRead(out var documentId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return documentId;
        }
    }
}

internal sealed class FakeRepository : IKnowledgeRepository
{
    public List<KnowledgeBaseDocument> Documents { get; } = new();
    public List<KnowledgeBaseChunk> Chunks { get; } = new();

    public Task<KnowledgeBaseDocument?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Documents.FirstOrDefault(d => d.Id == id));

    public Task<List<KnowledgeBaseDocument>> GetAllDocumentsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Documents.ToList());

    public Task AddDocumentAsync(KnowledgeBaseDocument document, CancellationToken cancellationToken = default)
    {
        Documents.Add(document);
        return Task.CompletedTask;
    }

    public Task UpdateDocumentAsync(KnowledgeBaseDocument document, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteDocumentAsync(KnowledgeBaseDocument document, CancellationToken cancellationToken = default)
    {
        Documents.Remove(document);
        Chunks.RemoveAll(c => c.KnowledgeBaseDocumentId == document.Id);
        return Task.CompletedTask;
    }

    public Task AddChunksAsync(List<KnowledgeBaseChunk> chunks, CancellationToken cancellationToken = default)
    {
        Chunks.AddRange(chunks);
        return Task.CompletedTask;
    }

    public Task<int> GetChunkCountAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Chunks.Count(c => c.KnowledgeBaseDocumentId == documentId));

    public Task DeleteAllChunksAsync(CancellationToken cancellationToken = default)
    {
        Chunks.Clear();
        return Task.CompletedTask;
    }

    public List<RetrievedChunkDto> KeywordHits { get; set; } = new();

    public IReadOnlyList<string>? LastKeywordTerms { get; private set; }

    public Task<List<RetrievedChunkDto>> SearchChunksByKeywordAsync(
        IReadOnlyList<string> terms,
        int limit,
        CancellationToken cancellationToken = default)
    {
        LastKeywordTerms = terms;

        return Task.FromResult(KeywordHits.Take(limit).ToList());
    }

    public Task<List<RetrievedChunkDto>> SearchChunksByEmbeddingAsync(
        Pgvector.Vector embedding,
        int limit = 5,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new List<RetrievedChunkDto>());
}
