using System;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DbContextAbstraction;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Infrastructure.Persistence;

// Fails startup if the live Embedding column width doesn't match Gemini:EmbeddingDimensions,
// before a bad insert or silently wrong retrieval can happen.
public sealed class EmbeddingDimensionGuard : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly GeminiOptions _options;
    private readonly ILogger<EmbeddingDimensionGuard> _logger;

    public EmbeddingDimensionGuard(
        IServiceScopeFactory scopeFactory,
        IOptions<GeminiOptions> options,
        ILogger<EmbeddingDimensionGuard> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var expected = _options.EmbeddingDimensions;
        int? actual;

        // Best-effort: an unreachable or un-migrated DB is inconclusive, not a mismatch, so it must not block startup.
        try
        {
            actual = await ReadColumnDimensionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not verify the embedding column dimension; continuing without the check.");
            return;
        }

        if (actual is null)
        {
            _logger.LogWarning(
                "Embedding column dimension could not be read; continuing without the check.");
            return;
        }

        // A proven mismatch fails closed - deliberately outside the try so it stops the host.
        EnsureMatch(actual.Value, expected);

        _logger.LogInformation(
            "Embedding column is vector({Dimension}); matches Gemini:EmbeddingDimensions.", actual.Value);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<int?> ReadColumnDimensionAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAIDbContext>();

        // The real implementation is an EF DbContext, so we can run the catalog query without depending on persistence.
        if (context is not DbContext db)
        {
            _logger.LogWarning("The AI DB context is not an EF DbContext; skipping the dimension check.");
            return null;
        }

        var formatType = await db.Database
            .SqlQueryRaw<string>(
                "SELECT format_type(a.atttypid, a.atttypmod) AS \"Value\" " +
                "FROM pg_attribute a " +
                "WHERE a.attrelid = 'ai.\"KnowledgeBaseChunk\"'::regclass AND a.attname = 'Embedding'")
            .FirstOrDefaultAsync(cancellationToken);

        return ParseDimension(formatType);
    }

    // Turns a pg type like "vector(1536)" into 1536.
    internal static int? ParseDimension(string? formatType)
    {
        if (string.IsNullOrWhiteSpace(formatType))
            return null;

        var open = formatType.IndexOf('(');
        var close = formatType.IndexOf(')');
        if (open < 0 || close <= open)
            return null;

        return int.TryParse(formatType.AsSpan(open + 1, close - open - 1), out var dimension)
            ? dimension
            : null;
    }

    internal static void EnsureMatch(int actual, int expected)
    {
        if (actual == expected)
            return;

        throw new InvalidOperationException(
            $"Embedding dimension mismatch: ai.\"KnowledgeBaseChunk\".\"Embedding\" is vector({actual}), " +
            $"but Gemini:EmbeddingDimensions is {expected}. Re-migrate the column to vector({expected}) " +
            "and re-embed the chunks before starting the app.");
    }
}
