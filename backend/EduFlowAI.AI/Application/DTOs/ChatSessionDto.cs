using System;
using System.Collections.Generic;

namespace EduFlowAI.AI.Application.DTOs;

public class ChatSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<ConversationTurnDto> History { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ConversationTurnDto
{
    public string Question { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;   // knowledge, application_status, document_status, recommendation
    public string RoutedTo { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
