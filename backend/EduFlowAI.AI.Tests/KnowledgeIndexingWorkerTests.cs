using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Application.Services;
using EduFlowAI.AI.Domain.Entities;
using EduFlowAI.AI.Domain.Enums;
using EduFlowAI.AI.Infrastructure.Indexing;
using EduFlowAI.AI.Infrastructure.Options;
using EduFlowAI.AI.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Tests;

public class KnowledgeIndexingWorkerTests
{
    private static readonly byte[] PdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nbody");

    [Fact]
    public async Task Worker_indexes_a_queued_document()
    {
        var ctx = Build();
        var documentId = await Upload(ctx, "track.pdf");

        await RunWorkerAsync(ctx);

        Assert.Equal(KnowledgeBaseStatus.Indexed, ctx.Repository.Documents.Single().Status);
        Assert.NotEmpty(ctx.Repository.Chunks);
    }

    // One unreadable document must not stall everything queued behind it.
    [Fact]
    public async Task A_failing_document_does_not_stop_the_queue()
    {
        var ctx = Build();
        ctx.Extractor.ThrowOnExtract = new InvalidOperationException("transcription unavailable");
        ctx.Extractor.FailForFileName = "broken.pdf";

        var failingId = await Upload(ctx, "broken.pdf");
        var goodId = await Upload(ctx, "track.pdf");

        await RunWorkerAsync(ctx);

        var failed = ctx.Repository.Documents.Single(d => d.Id == failingId);
        var indexed = ctx.Repository.Documents.Single(d => d.Id == goodId);
        Assert.Equal(KnowledgeBaseStatus.Failed, failed.Status);
        Assert.Equal("transcription unavailable", failed.ErrorMessage);
        Assert.Equal(KnowledgeBaseStatus.Indexed, indexed.Status);
    }

    [Fact]
    public async Task Worker_processes_every_queued_document()
    {
        var ctx = Build();
        await Upload(ctx, "one.pdf");
        await Upload(ctx, "two.pdf");
        await Upload(ctx, "three.pdf");

        await RunWorkerAsync(ctx);

        Assert.All(ctx.Repository.Documents, d => Assert.Equal(KnowledgeBaseStatus.Indexed, d.Status));
    }

    // The queue is in-memory, so a restart would otherwise leave these stuck forever.
    [Fact]
    public async Task Stranded_documents_are_requeued_on_startup()
    {
        var ctx = Build();
        ctx.Repository.Documents.Add(Stranded(KnowledgeBaseStatus.Pending, ctx));
        ctx.Repository.Documents.Add(Stranded(KnowledgeBaseStatus.Indexing, ctx));

        await RunWorkerAsync(ctx);

        Assert.All(ctx.Repository.Documents, d => Assert.Equal(KnowledgeBaseStatus.Indexed, d.Status));
    }

    [Fact]
    public async Task Already_indexed_documents_are_left_alone_on_startup()
    {
        var ctx = Build();
        var done = Stranded(KnowledgeBaseStatus.Indexed, ctx);
        ctx.Repository.Documents.Add(done);

        await RunWorkerAsync(ctx);

        Assert.Empty(ctx.Queue.Enqueued);
        Assert.Empty(ctx.Repository.Chunks);
    }

    [Fact]
    public async Task Startup_recovery_can_be_disabled()
    {
        var ctx = Build(options => options.RequeueStrandedOnStartup = false);
        ctx.Repository.Documents.Add(Stranded(KnowledgeBaseStatus.Pending, ctx));

        await RunWorkerAsync(ctx);

        Assert.Empty(ctx.Queue.Enqueued);
        Assert.Equal(KnowledgeBaseStatus.Pending, ctx.Repository.Documents.Single().Status);
    }

    private static KnowledgeBaseDocument Stranded(KnowledgeBaseStatus status, Context ctx)
    {
        var id = Guid.NewGuid();
        var storageKey = $"knowledge-base/{id}.pdf";
        ctx.Storage.Files[storageKey] = PdfBytes;

        return new KnowledgeBaseDocument
        {
            Id = id,
            FileName = "stranded.pdf",
            StorageKey = storageKey,
            UploadedByUserId = "system-seed",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static async Task<Guid> Upload(Context ctx, string fileName) =>
        await ctx.Service.AddDocumentAsync(new MemoryStream(PdfBytes), fileName, PdfBytes.Length);

    // FakeIndexingQueue ends once drained, so ExecuteTask completes instead of running forever.
    private static async Task RunWorkerAsync(Context ctx)
    {
        await ctx.Worker.StartAsync(CancellationToken.None);
        await ctx.Worker.ExecuteTask!;
        await ctx.Worker.StopAsync(CancellationToken.None);
    }

    private sealed record Context(
        KnowledgeIndexingWorker Worker,
        KnowledgeBaseService Service,
        FakeRepository Repository,
        FakeFileStorage Storage,
        FakeIndexingQueue Queue,
        FakeTextExtractor Extractor);

    private static Context Build(Action<IngestionOptions>? configure = null)
    {
        var options = new IngestionOptions();
        configure?.Invoke(options);

        var repository = new FakeRepository();
        var storage = new FakeFileStorage();
        var queue = new FakeIndexingQueue();
        var extractor = new FakeTextExtractor();

        var services = new ServiceCollection();
        services.AddSingleton<IDocumentTextExtractor>(extractor);
        services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
        services.AddSingleton<IKnowledgeRepository>(repository);
        services.AddSingleton<IFileStorageService>(storage);
        services.AddSingleton<IIndexingQueue>(queue);
        services.AddSingleton<KnowledgeBaseSyncState>();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<ILogger<KnowledgeBaseService>>(NullLogger<KnowledgeBaseService>.Instance);
        services.AddScoped<KnowledgeBaseService>();

        var provider = services.BuildServiceProvider();

        var worker = new KnowledgeIndexingWorker(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            NullLogger<KnowledgeIndexingWorker>.Instance);

        return new Context(
            worker,
            provider.GetRequiredService<KnowledgeBaseService>(),
            repository,
            storage,
            queue,
            extractor);
    }
}
