using EduFlowAI.Documents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.DbContextAbstraction
{
    public interface IDocumentDbContext
    {
        DbSet<ApplicantDocument> ApplicantDocuments { get; }
        DbSet<DocumentReplacementRequest> DocumentReplacementRequests { get; }
        DbSet<ApplicantDocumentVersion> ApplicantDocumentVersions { get; }

        Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
