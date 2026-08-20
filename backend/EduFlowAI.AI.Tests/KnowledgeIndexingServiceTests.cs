using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.Exceptions;
using EduFlowAI.AI.Application.Services;
using EduFlowAI.AI.Domain.Enums;
using EduFlowAI.AI.Infrastructure.Options;
using EduFlowAI.AI.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Tests;

// Covers the ingestion rules callers rely on, independent of any HTTP host.
public class KnowledgeIndexingServiceTests
{
    private static readonly byte[] PdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nbody");

    [Fact]
    public async Task AddDocument_rejects_an_empty_file()
    {
        var ctx = Build();

        await Assert.ThrowsAsync<KnowledgeBaseValidationException>(
            () => ctx.Service.AddDocumentAsync(new MemoryStream(), "empty.pdf", 0));
    }

    [Fact]
    public async Task AddDocument_rejects_a_file_over_the_size_cap()
    {
        var ctx = Build(options => options.MaxUploadBytes = 10);

        await Assert.ThrowsAsync<KnowledgeBaseTooLargeException>(
            () => ctx.Service.AddDocumentAsync(new MemoryStream(PdfBytes), "big.pdf", 11));
    }

    [Fact]
    public async Task AddDocument_rejects_a_non_pdf_renamed_to_pdf()
    {
        var ctx = Build();
        var disguised = new MemoryStream(Encoding.ASCII.GetBytes("PK not a pdf"));

        await Assert.ThrowsAsync<KnowledgeBaseValidationException>(
            () => ctx.Service.AddDocumentAsync(disguised, "invoice.pdf", disguised.Length));
    }

    // Validation must fail the caller directly, not turn into a Failed document to poll for.
    [Fact]
    public async Task Rejected_uploads_are_never_queued()
    {
        var ctx = Build();
        var disguised = new MemoryStream(Encoding.ASCII.GetBytes("PK not a pdf"));

        await Assert.ThrowsAsync<KnowledgeBaseValidationException>(
            () => ctx.Service.AddDocumentAsync(disguised, "invoice.pdf", disguised.Length));

        Assert.Empty(ctx.Queue.Enqueued);
        Assert.Empty(ctx.Repository.Documents);
    }

    [Fact]
    public async Task AddDocument_returns_pending_and_queues_without_indexing()
    {
        var ctx = Build();

        var documentId = await ctx.Service.AddDocumentAsync(new MemoryStream(PdfBytes), "track.pdf", PdfBytes.Length);

        var document = ctx.Repository.Documents.Single();
        Assert.Equal(documentId, document.Id);
        Assert.Equal(KnowledgeBaseStatus.Pending, document.Status);
        Assert.Equal(documentId, Assert.Single(ctx.Queue.Enqueued));
        Assert.Empty(ctx.Repository.Chunks);
    }

    [Fact]
    public async Task AddDocument_stores_the_original_before_queueing()
    {
        var ctx = Build();

        var documentId = await ctx.Service.AddDocumentAsync(new MemoryStream(PdfBytes), "track.pdf", PdfBytes.Length);

        var document = ctx.Repository.Documents.Single();
        Assert.Equal($"knowledge-base/{documentId}.pdf", document.StorageKey);
        Assert.True(ctx.Storage.Files.ContainsKey(document.StorageKey));
    }

    [Fact]
    public async Task Queued_document_reaches_indexed_once_processed()
    {
        var ctx = Build();
        var documentId = await ctx.Service.AddDocumentAsync(new MemoryStream(PdfBytes), "track.pdf", PdfBytes.Length);

        await ctx.Service.IngestDocumentAsync(documentId);

        Assert.Equal(KnowledgeBaseStatus.Indexed, ctx.Repository.Documents.Single().Status);
        Assert.NotEmpty(ctx.Repository.Chunks);
    }

    [Fact]
    public async Task AddDocument_keeps_the_text_extension_for_text_files()
    {
        var ctx = Build();
        var content = new MemoryStream(Encoding.UTF8.GetBytes("Admission opens in July."));

        var documentId = await ctx.Service.AddDocumentAsync(content, "faq.md", content.Length);

        Assert.Equal($"knowledge-base/{documentId}.md", ctx.Repository.Documents.Single().StorageKey);
    }

    [Fact]
    public async Task AddText_rejects_blank_content()
    {
        var ctx = Build();

        await Assert.ThrowsAsync<KnowledgeBaseValidationException>(() => ctx.Service.AddTextAsync("Note", "   "));
    }

    [Fact]
    public async Task AddText_stores_a_txt_file_so_resync_can_rebuild_it()
    {
        var ctx = Build();

        var documentId = await ctx.Service.AddTextAsync("Fees", "The application fee is 100 EGP.");

        var document = ctx.Repository.Documents.Single();
        Assert.Equal("Fees.txt", document.FileName);
        Assert.Equal($"knowledge-base/{documentId}.txt", document.StorageKey);
        Assert.True(ctx.Storage.Files.ContainsKey(document.StorageKey));
    }

    [Fact]
    public async Task AddText_is_queued_like_an_upload()
    {
        var ctx = Build();

        var documentId = await ctx.Service.AddTextAsync("Fees", "The application fee is 100 EGP.");

        Assert.Equal(documentId, Assert.Single(ctx.Queue.Enqueued));
        Assert.Equal(KnowledgeBaseStatus.Pending, ctx.Repository.Documents.Single().Status);
    }

    [Fact]
    public async Task AddText_falls_back_to_a_default_title()
    {
        var ctx = Build();

        await ctx.Service.AddTextAsync(null, "Some note.");

        Assert.Equal("Pasted note.txt", ctx.Repository.Documents.Single().FileName);
    }

    [Fact]
    public async Task Ingest_marks_the_document_failed_when_extraction_throws()
    {
        var ctx = Build();
        var documentId = await ctx.Service.AddDocumentAsync(new MemoryStream(PdfBytes), "track.pdf", PdfBytes.Length);
        ctx.Extractor.ThrowOnExtract = new InvalidOperationException("transcription unavailable");

        await Assert.ThrowsAsync<InvalidOperationException>(() => ctx.Service.IngestDocumentAsync(documentId));

        var document = ctx.Repository.Documents.Single();
        Assert.Equal(KnowledgeBaseStatus.Failed, document.Status);
        Assert.Equal("transcription unavailable", document.ErrorMessage);
    }

    [Fact]
    public async Task Delete_is_refused_while_a_resync_is_running()
    {
        var syncState = new KnowledgeBaseSyncState();
        var ctx = Build(syncState: syncState);
        syncState.TryBeginSync();

        await Assert.ThrowsAsync<KnowledgeBaseBusyException>(() => ctx.Service.DeleteDocumentAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_removes_the_row_and_the_stored_file()
    {
        var ctx = Build();
        var documentId = await ctx.Service.AddDocumentAsync(new MemoryStream(PdfBytes), "track.pdf", PdfBytes.Length);

        Assert.True(await ctx.Service.DeleteDocumentAsync(documentId));
        Assert.Empty(ctx.Repository.Documents);
        Assert.Empty(ctx.Storage.Files);
    }

    [Fact]
    public async Task Delete_reports_a_missing_document()
    {
        var ctx = Build();

        Assert.False(await ctx.Service.DeleteDocumentAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Resync_is_refused_while_another_one_is_running()
    {
        var syncState = new KnowledgeBaseSyncState();
        var ctx = Build(syncState: syncState);
        syncState.TryBeginSync();

        await Assert.ThrowsAsync<KnowledgeBaseBusyException>(() => ctx.Service.ResyncAllAsync());
    }

    [Fact]
    public async Task Resync_releases_the_gate_so_a_later_one_can_run()
    {
        var syncState = new KnowledgeBaseSyncState();
        var ctx = Build(syncState: syncState);

        await ctx.Service.ResyncAllAsync();

        Assert.False(syncState.IsSyncing);
    }

    // Re-sync stays synchronous - it rebuilds in place and reports a summary.
    [Fact]
    public async Task Resync_rebuilds_every_chunk_from_storage()
    {
        var ctx = Build();
        var documentId = await ctx.Service.AddDocumentAsync(new MemoryStream(PdfBytes), "track.pdf", PdfBytes.Length);
        await ctx.Service.IngestDocumentAsync(documentId);
        var firstPass = ctx.Repository.Chunks.Count;

        var result = await ctx.Service.ResyncAllAsync();

        Assert.Equal(1, result.TotalDocuments);
        Assert.Equal(1, result.Indexed);
        Assert.Equal(0, result.Failed);
        Assert.Equal(firstPass, ctx.Repository.Chunks.Count);
    }

    // A resync wipes every chunk first, so it is refused when a source file is missing.
    [Fact]
    public async Task Resync_refuses_and_deletes_nothing_when_a_source_file_is_gone()
    {
        var ctx = Build();
        var documentId = await ctx.Service.AddDocumentAsync(new MemoryStream(PdfBytes), "track.pdf", PdfBytes.Length);
        await ctx.Service.IngestDocumentAsync(documentId);
        var indexedChunks = ctx.Repository.Chunks.Count;
        Assert.True(indexedChunks > 0);

        ctx.Storage.Files.Clear();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ctx.Service.ResyncAllAsync());

        Assert.Contains("Nothing was deleted", ex.Message);
        Assert.Contains("track.pdf", ex.Message);
        Assert.Equal(indexedChunks, ctx.Repository.Chunks.Count);
    }

    [Fact]
    public async Task Status_reports_the_chunk_count()
    {
        var ctx = Build();
        var documentId = await ctx.Service.AddDocumentAsync(new MemoryStream(PdfBytes), "track.pdf", PdfBytes.Length);
        await ctx.Service.IngestDocumentAsync(documentId);

        var status = await ctx.Service.GetDocumentStatusAsync(documentId);

        Assert.NotNull(status);
        Assert.Equal("Indexed", status!.Status);
        Assert.Equal(ctx.Repository.Chunks.Count, status.ChunkCount);
    }

    [Fact]
    public async Task Status_starts_at_pending_with_no_chunks()
    {
        var ctx = Build();
        var documentId = await ctx.Service.AddDocumentAsync(new MemoryStream(PdfBytes), "track.pdf", PdfBytes.Length);

        var status = await ctx.Service.GetDocumentStatusAsync(documentId);

        Assert.Equal("Pending", status!.Status);
        Assert.Equal(0, status.ChunkCount);
    }

    [Fact]
    public async Task Status_is_null_for_an_unknown_document()
    {
        var ctx = Build();

        Assert.Null(await ctx.Service.GetDocumentStatusAsync(Guid.NewGuid()));
    }

    private sealed record Context(
        KnowledgeBaseService Service,
        FakeRepository Repository,
        FakeFileStorage Storage,
        FakeIndexingQueue Queue,
        FakeTextExtractor Extractor);

    private static Context Build(
        Action<IngestionOptions>? configure = null,
        KnowledgeBaseSyncState? syncState = null)
    {
        var options = new IngestionOptions();
        configure?.Invoke(options);

        var repository = new FakeRepository();
        var storage = new FakeFileStorage();
        var queue = new FakeIndexingQueue();
        var extractor = new FakeTextExtractor();

        var service = new KnowledgeBaseService(
            extractor,
            new FakeEmbeddingService(),
            repository,
            storage,
            queue,
            syncState ?? new KnowledgeBaseSyncState(),
            Options.Create(options),
            NullLogger<KnowledgeBaseService>.Instance);

        return new Context(service, repository, storage, queue, extractor);
    }
}
