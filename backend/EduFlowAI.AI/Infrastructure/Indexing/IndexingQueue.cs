using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.Interfaces;

namespace EduFlowAI.AI.Infrastructure.Indexing;

public class IndexingQueue : IIndexingQueue
{
    // Unbounded: an upload must never block waiting for a slot, and the volume here is admin-driven.
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true
    });

    public ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(documentId, cancellationToken);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
