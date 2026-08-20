using System;

namespace EduFlowAI.AI.Application.DTOs;

public class KnowledgeBaseDocumentDto
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
