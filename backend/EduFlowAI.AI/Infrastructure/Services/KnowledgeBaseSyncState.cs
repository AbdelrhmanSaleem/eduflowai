using System.Threading;

namespace EduFlowAI.AI.Infrastructure.Services;

// A re-sync deletes and rebuilds every chunk, so chat is paused while one runs.
public class KnowledgeBaseSyncState
{
    private int _syncing;

    public bool IsSyncing => Volatile.Read(ref _syncing) == 1;

    public bool TryBeginSync() => Interlocked.CompareExchange(ref _syncing, 1, 0) == 0;

    public void EndSync() => Volatile.Write(ref _syncing, 0);
}
