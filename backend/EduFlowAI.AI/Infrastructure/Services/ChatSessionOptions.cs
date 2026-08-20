using System;

namespace EduFlowAI.AI.Infrastructure.Services;

public class ChatSessionOptions
{
    public const string SectionName = "ChatSession";

    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(30);

    // 0 = unlimited.
    public int MaxHistoryTurns { get; set; } = 50;
}
