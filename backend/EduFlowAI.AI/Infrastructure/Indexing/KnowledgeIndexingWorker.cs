using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Application.Services;
using EduFlowAI.AI.Domain.Enums;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Infrastructure.Indexing;

public class KnowledgeIndexingWorker : BackgroundService
{
    private readonly IIndexingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IngestionOptions _options;
    private readonly ILogger<KnowledgeIndexingWorker> _logger;

    public KnowledgeIndexingWorker(
        IIndexingQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<IngestionOptions> options,
        ILogger<KnowledgeIndexingWorker> logger)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options?.Value ?? new IngestionOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.RequeueStrandedOnStartup)
            await RequeueStrandedDocumentsAsync(stoppingToken);

        await foreach (var documentId in _queue.DequeueAllAsync(stoppingToken))
        {
            await IndexOneAsync(documentId, stoppingToken);
        }
    }

    // The queue lives in memory, so a restart mid-index leaves documents stuck in Pending/Indexing.
    private async Task RequeueStrandedDocumentsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IKnowledgeRepository>();

            var stranded = (await repository.GetAllDocumentsAsync(cancellationToken))
                .Where(d => d.Status is KnowledgeBaseStatus.Pending or KnowledgeBaseStatus.Indexing)
                .ToList();

            foreach (var document in stranded)
                await _queue.EnqueueAsync(document.Id, cancellationToken);

            if (stranded.Count > 0)
                _logger.LogInformation("Re-queued {Count} document(s) left unindexed by a previous run.", stranded.Count);
        }
        catch (Exception ex)
        {
            // Recovery is best-effort - never stop the worker starting because of it.
            _logger.LogError(ex, "Could not re-queue stranded documents on startup.");
        }
    }

    private async Task IndexOneAsync(Guid documentId, CancellationToken cancellationToken)
    {
        try
        {
            // Queue is a singleton, indexing service is scoped: each document gets its own scope and DbContext.
            using var scope = _scopeFactory.CreateScope();
            var knowledgeBase = scope.ServiceProvider.GetRequiredService<KnowledgeBaseService>();

            await knowledgeBase.IngestDocumentAsync(documentId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // IngestDocumentAsync already recorded Failed + ErrorMessage; swallow so one bad document doesn't stop the queue.
            _logger.LogError(ex, "Indexing failed for document {DocumentId}.", documentId);
        }
    }
}
