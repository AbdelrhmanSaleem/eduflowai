using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EduFlowAI.AI.Application.Interfaces;

// Uploads return as soon as the document row exists; the indexing itself happens off the request.
public interface IIndexingQueue
{
    ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}
