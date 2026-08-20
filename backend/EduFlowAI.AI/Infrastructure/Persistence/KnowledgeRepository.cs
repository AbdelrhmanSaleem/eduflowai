using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using EduFlowAI.AI.Application.DbContextAbstraction;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Domain.Entities;

namespace EduFlowAI.AI.Infrastructure.Persistence;

public class KnowledgeRepository : IKnowledgeRepository
{
    // Upper bound on rows pulled back for in-memory keyword ranking.
    private const int MaxKeywordCandidates = 200;

    private readonly IAIDbContext _context;

    public KnowledgeRepository(IAIDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<KnowledgeBaseDocument?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgeBaseDocuments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<List<KnowledgeBaseDocument>> GetAllDocumentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgeBaseDocuments.ToListAsync(cancellationToken);
    }

    public async Task AddDocumentAsync(KnowledgeBaseDocument document, CancellationToken cancellationToken = default)
    {
        await _context.KnowledgeBaseDocuments.AddAsync(document, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDocumentAsync(KnowledgeBaseDocument document, CancellationToken cancellationToken = default)
    {
        _context.KnowledgeBaseDocuments.Update(document);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDocumentAsync(KnowledgeBaseDocument document, CancellationToken cancellationToken = default)
    {
        // Chunks go with it through the FK cascade.
        _context.KnowledgeBaseDocuments.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllChunksAsync(CancellationToken cancellationToken = default)
    {
        await _context.KnowledgeBaseChunks.ExecuteDeleteAsync(cancellationToken);
    }

    public async Task AddChunksAsync(List<KnowledgeBaseChunk> chunks, CancellationToken cancellationToken = default)
    {
        await _context.KnowledgeBaseChunks.AddRangeAsync(chunks, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetChunkCountAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgeBaseChunks
            .CountAsync(c => c.KnowledgeBaseDocumentId == documentId, cancellationToken);
    }

    public async Task<List<RetrievedChunkDto>> SearchChunksByEmbeddingAsync(
        Pgvector.Vector embedding,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgeBaseChunks
            .OrderBy(c => c.Embedding.CosineDistance(embedding))
            .Take(limit)
            .Select(c => new RetrievedChunkDto
            {
                ChunkId = c.Id,
                Content = c.Content,
                DocumentId = c.KnowledgeBaseDocumentId,
                SourceTitle = c.Document.FileName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RetrievedChunkDto>> SearchChunksByKeywordAsync(
        IReadOnlyList<string> terms,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (terms is null || terms.Count == 0 || limit <= 0)
            return new List<RetrievedChunkDto>();

        var patterns = terms.Select(term => $"%{term}%").ToList();

        // Match ANY term in SQL, then rank in memory; the cap keeps it bounded.
        var candidates = await _context.KnowledgeBaseChunks
            .Where(c => patterns.Any(pattern => EF.Functions.ILike(c.Content, pattern)))
            .Take(MaxKeywordCandidates)
            .Select(c => new RetrievedChunkDto
            {
                ChunkId = c.Id,
                Content = c.Content,
                DocumentId = c.KnowledgeBaseDocumentId,
                SourceTitle = c.Document.FileName
            })
            .ToListAsync(cancellationToken);

        // A chunk naming several terms beats one repeating a common word.
        return candidates
            .OrderByDescending(c => terms.Count(term =>
                c.Content.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Take(limit)
            .ToList();
    }
}
