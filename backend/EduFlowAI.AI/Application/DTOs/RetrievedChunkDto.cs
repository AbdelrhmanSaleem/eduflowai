using System;

namespace EduFlowAI.AI.Application.DTOs;

public class RetrievedChunkDto
{
    // Identifies the chunk so vector and keyword hits for the same chunk collapse into one.
    public Guid ChunkId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string SourceTitle { get; set; } = string.Empty;

    public Guid DocumentId { get; set; }
}
