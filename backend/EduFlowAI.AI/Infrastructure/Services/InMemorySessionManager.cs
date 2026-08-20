using System;
using System.Collections.Generic;
using System.Linq;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Infrastructure.Services;

public class InMemorySessionManager : ISessionManager
{
    private readonly Dictionary<string, ChatSessionDto> _sessions = new();
    private readonly object _lockObject = new();
    private readonly ChatSessionOptions _options;
    private readonly TimeProvider _clock;

    public InMemorySessionManager(IOptions<ChatSessionOptions>? options = null, TimeProvider? clock = null)
    {
        _options = options?.Value ?? new ChatSessionOptions();
        _clock = clock ?? TimeProvider.System;
    }

    public ChatSessionDto CreateSession(string userId)
    {
        lock (_lockObject)
        {
            var now = _clock.GetUtcNow();
            SweepExpired(now);

            var session = new ChatSessionDto
            {
                SessionId = Guid.NewGuid().ToString(),
                UserId = userId,
                CreatedAt = now,
                LastActivityAt = now
            };
            _sessions[session.SessionId] = session;
            return session;
        }
    }

    public ChatSessionDto? GetSession(string sessionId)
    {
        lock (_lockObject)
        {
            return GetLiveSession(sessionId);
        }
    }

    public bool AddTurn(string sessionId, ConversationTurnDto turn)
    {
        lock (_lockObject)
        {
            var session = GetLiveSession(sessionId);
            if (session == null)
                return false;

            session.History.Add(turn);

            var max = _options.MaxHistoryTurns;
            if (max > 0 && session.History.Count > max)
                session.History.RemoveRange(0, session.History.Count - max);

            session.LastActivityAt = _clock.GetUtcNow();
            return true;
        }
    }

    public List<ConversationTurnDto> GetHistory(string sessionId, int lastN = 3)
    {
        lock (_lockObject)
        {
            var session = GetLiveSession(sessionId);
            if (session == null)
                return new List<ConversationTurnDto>();

            return session.History
                .Skip(Math.Max(0, session.History.Count - lastN))
                .ToList();
        }
    }

    public void EndSession(string sessionId)
    {
        lock (_lockObject)
        {
            _sessions.Remove(sessionId);
        }
    }

    private ChatSessionDto? GetLiveSession(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return null;

        if (IsExpired(session.LastActivityAt, _clock.GetUtcNow(), _options.IdleTimeout))
        {
            _sessions.Remove(sessionId);
            return null;
        }

        return session;
    }

    private void SweepExpired(DateTimeOffset now)
    {
        var expired = _sessions
            .Where(kv => IsExpired(kv.Value.LastActivityAt, now, _options.IdleTimeout))
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in expired)
            _sessions.Remove(key);
    }

    internal static bool IsExpired(DateTimeOffset lastActivityAt, DateTimeOffset nowUtc, TimeSpan idleTimeout)
        => idleTimeout > TimeSpan.Zero && (nowUtc - lastActivityAt) >= idleTimeout;
}
