using System;
using System.Collections.Generic;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Tests;

public class InMemorySessionManagerTests
{
    private static ConversationTurnDto Turn(string q) => new() { Question = q, Answer = "a" };

    // Controllable clock so expiry is deterministic without sleeping.
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static InMemorySessionManager WithOptions(ChatSessionOptions opts, TimeProvider? clock = null)
        => new(Options.Create(opts), clock);

    [Fact]
    public void CreateSession_ReturnsSessionWithIdUserAndEmptyHistory()
    {
        var mgr = new InMemorySessionManager();
        var userId = Guid.NewGuid().ToString();

        var session = mgr.CreateSession(userId);

        Assert.False(string.IsNullOrWhiteSpace(session.SessionId));
        Assert.Equal(userId, session.UserId);
        Assert.Empty(session.History);
    }

    [Fact]
    public void GetSession_UnknownId_ReturnsNull()
    {
        var mgr = new InMemorySessionManager();
        Assert.Null(mgr.GetSession("does-not-exist"));
    }

    [Fact]
    public void AddTurn_AppendsToHistory()
    {
        var mgr = new InMemorySessionManager();
        var session = mgr.CreateSession(Guid.NewGuid().ToString());

        mgr.AddTurn(session.SessionId, Turn("Q1"));

        var history = mgr.GetHistory(session.SessionId, lastN: 10);
        Assert.Single(history);
        Assert.Equal("Q1", history[0].Question);
    }

    [Fact]
    public void GetHistory_ReturnsLastNInChronologicalOrder()
    {
        var mgr = new InMemorySessionManager();
        var session = mgr.CreateSession(Guid.NewGuid().ToString());
        for (int i = 1; i <= 5; i++)
            mgr.AddTurn(session.SessionId, Turn($"Q{i}"));

        var last3 = mgr.GetHistory(session.SessionId, lastN: 3);

        Assert.Equal(3, last3.Count);
        Assert.Equal("Q3", last3[0].Question);
        Assert.Equal("Q4", last3[1].Question);
        Assert.Equal("Q5", last3[2].Question);
    }

    [Fact]
    public void GetHistory_LastNLargerThanCount_ReturnsAllTurns()
    {
        var mgr = new InMemorySessionManager();
        var session = mgr.CreateSession(Guid.NewGuid().ToString());
        mgr.AddTurn(session.SessionId, Turn("only"));

        Assert.Single(mgr.GetHistory(session.SessionId, lastN: 100));
    }

    [Fact]
    public void GetHistory_UnknownSession_ReturnsEmpty()
    {
        var mgr = new InMemorySessionManager();
        Assert.Empty(mgr.GetHistory("missing", lastN: 3));
    }

    [Fact]
    public void EndSession_RemovesSession()
    {
        var mgr = new InMemorySessionManager();
        var session = mgr.CreateSession(Guid.NewGuid().ToString());

        mgr.EndSession(session.SessionId);

        Assert.Null(mgr.GetSession(session.SessionId));
    }

    [Fact]
    public void EndSession_CalledTwice_DoesNotThrow()
    {
        var mgr = new InMemorySessionManager();
        var session = mgr.CreateSession(Guid.NewGuid().ToString());

        mgr.EndSession(session.SessionId);
        mgr.EndSession(session.SessionId); // second removal is a no-op

        Assert.Null(mgr.GetSession(session.SessionId));
    }

    [Fact]
    public void SeparateSessions_KeepIndependentHistories()
    {
        var mgr = new InMemorySessionManager();
        var a = mgr.CreateSession(Guid.NewGuid().ToString());
        var b = mgr.CreateSession(Guid.NewGuid().ToString());

        mgr.AddTurn(a.SessionId, Turn("a-only"));

        Assert.Single(mgr.GetHistory(a.SessionId, lastN: 10));
        Assert.Empty(mgr.GetHistory(b.SessionId, lastN: 10));
    }

    [Fact]
    public void AddTurn_UnknownSession_ReturnsFalseAndDoesNotThrow()
    {
        var mgr = new InMemorySessionManager();
        Assert.False(mgr.AddTurn("missing", Turn("x")));
    }

    [Fact]
    public void AddTurn_KnownSession_ReturnsTrue()
    {
        var mgr = new InMemorySessionManager();
        var s = mgr.CreateSession(Guid.NewGuid().ToString());
        Assert.True(mgr.AddTurn(s.SessionId, Turn("hi")));
    }

    [Fact]
    public void IdleSession_PastTimeout_IsExpiredAndEvictedOnAccess()
    {
        var clock = new TestClock { Now = DateTimeOffset.UtcNow };
        var mgr = WithOptions(new ChatSessionOptions { IdleTimeout = TimeSpan.FromMinutes(30) }, clock);
        var s = mgr.CreateSession(Guid.NewGuid().ToString());

        clock.Now = clock.Now.AddMinutes(31); // idle past the timeout

        Assert.Null(mgr.GetSession(s.SessionId));           // expired -> gone
        Assert.False(mgr.AddTurn(s.SessionId, Turn("late"))); // and no longer writable
    }

    [Fact]
    public void Activity_RefreshesExpiryWindow()
    {
        var clock = new TestClock { Now = DateTimeOffset.UtcNow };
        var mgr = WithOptions(new ChatSessionOptions { IdleTimeout = TimeSpan.FromMinutes(30) }, clock);
        var s = mgr.CreateSession(Guid.NewGuid().ToString());

        clock.Now = clock.Now.AddMinutes(20);
        Assert.True(mgr.AddTurn(s.SessionId, Turn("keepalive"))); // refreshes LastActivityAt
        clock.Now = clock.Now.AddMinutes(20);                     // 40 min from creation, 20 from last activity

        Assert.NotNull(mgr.GetSession(s.SessionId)); // still alive because activity refreshed the window
    }

    [Fact]
    public void History_IsCappedToMaxTurns_DroppingOldest()
    {
        var mgr = WithOptions(new ChatSessionOptions { MaxHistoryTurns = 3 });
        var s = mgr.CreateSession(Guid.NewGuid().ToString());

        for (int i = 1; i <= 5; i++)
            mgr.AddTurn(s.SessionId, Turn($"Q{i}"));

        var all = mgr.GetHistory(s.SessionId, lastN: 100);
        Assert.Equal(3, all.Count);
        Assert.Equal("Q3", all[0].Question); // oldest two (Q1, Q2) trimmed
        Assert.Equal("Q5", all[2].Question);
    }

    [Fact]
    public void IsExpired_PureLogic()
    {
        var t0 = DateTimeOffset.UtcNow;
        Assert.False(InMemorySessionManager.IsExpired(t0, t0.AddMinutes(29), TimeSpan.FromMinutes(30)));
        Assert.True(InMemorySessionManager.IsExpired(t0, t0.AddMinutes(30), TimeSpan.FromMinutes(30)));
        Assert.False(InMemorySessionManager.IsExpired(t0, t0.AddYears(1), TimeSpan.Zero)); // 0 = never expire
    }
}
