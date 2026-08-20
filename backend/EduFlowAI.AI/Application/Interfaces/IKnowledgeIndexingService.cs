using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DTOs;

namespace EduFlowAI.AI.Application.Interfaces;

public interface IKnowledgeIndexingService
{
    // Validates, stores and indexes a PDF / .txt / .md file, then returns the new document id.
    // Throws KnowledgeBaseValidationException (400) or KnowledgeBaseTooLargeException (413).
    Task<Guid> AddDocumentAsync(
        Stream content,
        string fileName,
        long lengthInBytes,
        CancellationToken cancellationToken = default);

    // Same pipeline for text typed straight into the admin screen.
    Task<Guid> AddTextAsync(string? title, string content, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeBaseDocumentDto>> GetDocumentsAsync(CancellationToken cancellationToken = default);

    // Null when no document has that id.
    Task<KnowledgeBaseDocumentStatusDto?> GetDocumentStatusAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    // False when no document has that id. Throws KnowledgeBaseBusyException (409) during a re-sync.
    Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    // Rebuilds every embedding from the stored originals.
    // Throws KnowledgeBaseBusyException (409) if one is already running.
    Task<KnowledgeBaseSyncResultDto> ResyncAllAsync(CancellationToken cancellationToken = default);
}
