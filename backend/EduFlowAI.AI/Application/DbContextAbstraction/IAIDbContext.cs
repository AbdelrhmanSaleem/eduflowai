using EduFlowAI.AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.AI.Application.DbContextAbstraction
{
    public interface IAIDbContext
    {
        DbSet<KnowledgeBaseDocument> KnowledgeBaseDocuments { get; }
        DbSet<KnowledgeBaseChunk> KnowledgeBaseChunks { get; }

        Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
