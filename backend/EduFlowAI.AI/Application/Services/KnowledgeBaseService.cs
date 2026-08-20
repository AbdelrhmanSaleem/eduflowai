using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Exceptions;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Domain.Entities;
using EduFlowAI.AI.Domain.Enums;
using EduFlowAI.AI.Infrastructure.ExternalServices;
using EduFlowAI.AI.Infrastructure.Options;
using EduFlowAI.AI.Infrastructure.Processing;
using EduFlowAI.AI.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Application.Services;

public class KnowledgeBaseService : IKnowledgeIndexingService
{
    // Until Identity lands there is no authenticated uploader to record.
    private const string PlaceholderUploaderId = "system-seed";

    private readonly IDocumentTextExtractor _textExtractor;
    private readonly IEmbeddingService _embeddingService;
    private readonly IKnowledgeRepository _repository;
    private readonly IFileStorageService _fileStorage;
    private readonly IIndexingQueue _indexingQueue;
    private readonly KnowledgeBaseSyncState _syncState;
    private readonly IngestionOptions _ingestionOptions;
    private readonly ILogger<KnowledgeBaseService> _logger;

    public KnowledgeBaseService(
        IDocumentTextExtractor textExtractor,
        IEmbeddingService embeddingService,
        IKnowledgeRepository repository,
        IFileStorageService fileStorage,
        IIndexingQueue indexingQueue,
        KnowledgeBaseSyncState syncState,
        IOptions<IngestionOptions> ingestionOptions,
        ILogger<KnowledgeBaseService> logger)
    {
        _textExtractor = textExtractor ?? throw new ArgumentNullException(nameof(textExtractor));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _indexingQueue = indexingQueue ?? throw new ArgumentNullException(nameof(indexingQueue));
        _syncState = syncState ?? throw new ArgumentNullException(nameof(syncState));
        _ingestionOptions = ingestionOptions?.Value ?? new IngestionOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Guid> AddDocumentAsync(
        Stream content,
        string fileName,
        long lengthInBytes,
        CancellationToken cancellationToken = default)
    {
        if (content == null || lengthInBytes == 0)
            throw new KnowledgeBaseValidationException("No file was uploaded or the file is completely empty.");

        if (lengthInBytes > _ingestionOptions.MaxUploadBytes)
            throw new KnowledgeBaseTooLargeException(
                $"The uploaded file exceeds the {_ingestionOptions.MaxUploadBytes / (1024 * 1024)} MB limit.");

        var isPlainText = DocumentTextExtractor.IsPlainText(fileName);

        // Renaming a file to .pdf cannot fake the magic header.
        if (!isPlainText && !await PdfFileValidation.HasPdfHeaderAsync(content, cancellationToken))
            throw new KnowledgeBaseValidationException("Unsupported file. Upload a PDF, or a .txt / .md text file.");

        var extension = isPlainText ? Path.GetExtension(fileName).ToLowerInvariant() : ".pdf";
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".txt";

        return await StoreAndIndexAsync(content, fileName, extension, cancellationToken);
    }

    public async Task<Guid> AddTextAsync(string? title, string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new KnowledgeBaseValidationException("Content cannot be empty.");

        var name = string.IsNullOrWhiteSpace(title) ? "Pasted note" : title.Trim();

        // The .txt suffix tells the extractor to decode rather than transcribe.
        if (!name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            name = $"{name}.txt";

        using var buffer = new MemoryStream(Encoding.UTF8.GetBytes(content));

        return await StoreAndIndexAsync(buffer, name, ".txt", cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeBaseDocumentDto>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var documents = await _repository.GetAllDocumentsAsync(cancellationToken);

        return documents
            .Select(d => new KnowledgeBaseDocumentDto
            {
                DocumentId = d.Id,
                FileName = d.FileName,
                Status = d.Status.ToString(),
                ErrorMessage = d.ErrorMessage,
                CreatedAt = d.CreatedAt
            })
            .ToList();
    }

    public async Task<KnowledgeBaseDocumentStatusDto?> GetDocumentStatusAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _repository.GetDocumentByIdAsync(documentId, cancellationToken);
        if (document == null)
            return null;

        return new KnowledgeBaseDocumentStatusDto
        {
            DocumentId = document.Id,
            FileName = document.FileName,
            Status = document.Status.ToString(),
            ErrorMessage = document.ErrorMessage,
            ChunkCount = await _repository.GetChunkCountAsync(documentId, cancellationToken),
            CreatedAt = document.CreatedAt
        };
    }

    private async Task<Guid> StoreAndIndexAsync(
        Stream content,
        string fileName,
        string extension,
        CancellationToken cancellationToken)
    {
        var documentId = Guid.NewGuid();
        var storageKey = $"knowledge-base/{documentId}{extension}";

        await _fileStorage.SaveAsync(content, storageKey, cancellationToken);

        var document = new KnowledgeBaseDocument
        {
            Id = documentId,
            FileName = fileName,
            StorageKey = storageKey,
            UploadedByUserId = PlaceholderUploaderId,
            Status = KnowledgeBaseStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddDocumentAsync(document, cancellationToken);

        // Indexing takes ~20s, so return the id as Pending now; the caller polls until Indexed/Failed.
        await _indexingQueue.EnqueueAsync(documentId, cancellationToken);

        return documentId;
    }

    public async Task IngestDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _repository.GetDocumentByIdAsync(documentId, cancellationToken);

        if (document == null)
            throw new KeyNotFoundException($"Knowledge Base Document with ID {documentId} was not found.");

        try
        {
            await IndexDocumentAsync(document, cancellationToken);
        }
        catch (Exception ex)
        {
            await MarkFailedAsync(document, ex, cancellationToken);
            throw;
        }
    }

    public async Task<KnowledgeBaseSyncResultDto> ResyncAllAsync(CancellationToken cancellationToken = default)
    {
        if (!_syncState.TryBeginSync())
            throw new KnowledgeBaseBusyException("A knowledge-base sync is already in progress.");

        try
        {
            return await RunResyncAsync(cancellationToken);
        }
        finally
        {
            _syncState.EndSync();
        }
    }

    private async Task<KnowledgeBaseSyncResultDto> RunResyncAsync(CancellationToken cancellationToken)
    {
        var documents = await _repository.GetAllDocumentsAsync(cancellationToken);

        // Refuse before deleting: a wipe with missing sources destroys chunks that cannot be rebuilt.
        var missing = documents
            .Where(d => !string.IsNullOrWhiteSpace(d.StorageKey) && !_fileStorage.Exists(d.StorageKey))
            .Select(d => d.FileName)
            .ToList();

        if (missing.Count > 0)
        {
            _logger.LogError(
                "Resync aborted: {MissingCount} of {TotalCount} source files are missing ({Missing}).",
                missing.Count, documents.Count, string.Join(", ", missing.Take(5)));

            throw new InvalidOperationException(
                $"Resync aborted: {missing.Count} of {documents.Count} source files are missing from " +
                $"storage (for example: {string.Join(", ", missing.Take(3))}). Nothing was deleted - " +
                "restore the files, or delete and re-upload those documents individually.");
        }

        // Wipe first so a failed document cannot leave stale vectors behind.
        await _repository.DeleteAllChunksAsync(cancellationToken);

        var result = new KnowledgeBaseSyncResultDto { TotalDocuments = documents.Count };

        foreach (var document in documents)
        {
            try
            {
                await IndexDocumentAsync(document, cancellationToken);
                result.Indexed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Re-sync failed for document {DocumentId} ({FileName})", document.Id, document.FileName);
                await MarkFailedAsync(document, ex, cancellationToken);
                result.Failed++;
            }
        }

        _logger.LogInformation(
            "Knowledge base re-sync finished: {Indexed} indexed, {Failed} failed of {Total}.",
            result.Indexed, result.Failed, result.TotalDocuments);

        return result;
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Would race the rebuild currently writing chunks.
        if (_syncState.IsSyncing)
            throw new KnowledgeBaseBusyException("A knowledge-base sync is in progress. Try again once it completes.");

        var document = await _repository.GetDocumentByIdAsync(documentId, cancellationToken);
        if (document == null)
            return false;

        var storageKey = document.StorageKey;

        await _repository.DeleteDocumentAsync(document, cancellationToken);

        // Only after the row is gone, so a DB failure cannot orphan the record.
        if (!string.IsNullOrWhiteSpace(storageKey))
            await _fileStorage.DeleteAsync(storageKey, cancellationToken);

        return true;
    }

    private async Task IndexDocumentAsync(KnowledgeBaseDocument document, CancellationToken cancellationToken)
    {
        document.Status = KnowledgeBaseStatus.Indexing;
        await _repository.UpdateDocumentAsync(document, cancellationToken);

        byte[] content;
        await using (var stored = await _fileStorage.OpenReadAsync(document.StorageKey, cancellationToken))
        {
            using var buffer = new MemoryStream();
            await stored.CopyToAsync(buffer, cancellationToken);
            content = buffer.ToArray();
        }

        var text = await _textExtractor.ExtractTextAsync(content, document.FileName, cancellationToken);

        var chunks = TextChunker.Split(text, _ingestionOptions.MaxChunkChars, _ingestionOptions.ChunkOverlapChars);

        if (chunks.Count == 0)
            throw new InvalidOperationException("The document did not yield any readable text.");

        // Stamp each chunk so a fragment split from its heading still says what it belongs to.
        chunks = ChunkContextualizer.Contextualize(document.FileName, text, chunks);

        var embeddedChunks = new List<KnowledgeBaseChunk>();
        foreach (var chunkText in chunks)
        {
            var coordinateArray = await _embeddingService.GenerateEmbeddingAsync(chunkText, cancellationToken);

            embeddedChunks.Add(new KnowledgeBaseChunk
            {
                KnowledgeBaseDocumentId = document.Id,
                Content = chunkText,
                Embedding = new Pgvector.Vector(coordinateArray)
            });
        }

        await _repository.AddChunksAsync(embeddedChunks, cancellationToken);

        document.Status = KnowledgeBaseStatus.Indexed;
        document.ErrorMessage = null;
        await _repository.UpdateDocumentAsync(document, cancellationToken);

        _logger.LogInformation("Indexed {FileName} into {ChunkCount} chunks.", document.FileName, embeddedChunks.Count);
    }

    private async Task MarkFailedAsync(KnowledgeBaseDocument document, Exception ex, CancellationToken cancellationToken)
    {
        document.Status = KnowledgeBaseStatus.Failed;
        document.ErrorMessage = ex.Message;
        await _repository.UpdateDocumentAsync(document, cancellationToken);
    }
}
