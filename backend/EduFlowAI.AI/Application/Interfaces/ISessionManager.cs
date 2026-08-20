using System;
using System.Collections.Generic;
using EduFlowAI.AI.Application.DTOs;

namespace EduFlowAI.AI.Application.Interfaces;

public interface ISessionManager
{
    ChatSessionDto CreateSession(string userId);
    ChatSessionDto? GetSession(string sessionId);

    // False when the session is missing or expired.
    bool AddTurn(string sessionId, ConversationTurnDto turn);

    List<ConversationTurnDto> GetHistory(string sessionId, int lastN = 3);
    void EndSession(string sessionId);
}
