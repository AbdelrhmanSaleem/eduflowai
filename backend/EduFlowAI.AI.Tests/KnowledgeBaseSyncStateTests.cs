using System.Linq;
using System.Threading.Tasks;
using EduFlowAI.AI.Infrastructure.Services;

namespace EduFlowAI.AI.Tests;

public class KnowledgeBaseSyncStateTests
{
    [Fact]
    public void StartsIdle()
    {
        Assert.False(new KnowledgeBaseSyncState().IsSyncing);
    }

    [Fact]
    public void TryBeginSync_ClaimsTheSlot()
    {
        var state = new KnowledgeBaseSyncState();

        Assert.True(state.TryBeginSync());
        Assert.True(state.IsSyncing);
    }

    [Fact]
    public void TryBeginSync_SecondCall_IsRejectedWhileRunning()
    {
        var state = new KnowledgeBaseSyncState();
        state.TryBeginSync();

        Assert.False(state.TryBeginSync()); // prevents two concurrent rebuilds
    }

    [Fact]
    public void EndSync_ReleasesTheSlot()
    {
        var state = new KnowledgeBaseSyncState();
        state.TryBeginSync();

        state.EndSync();

        Assert.False(state.IsSyncing);
        Assert.True(state.TryBeginSync()); // claimable again
    }

    [Fact]
    public async Task ConcurrentBegin_OnlyOneWins()
    {
        var state = new KnowledgeBaseSyncState();

        var results = await Task.WhenAll(
            Enumerable.Range(0, 32).Select(_ => Task.Run(() => state.TryBeginSync())));

        Assert.Equal(1, results.Count(won => won));
    }
}
