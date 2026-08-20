using EduFlowAI.AI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.AI.Domain.Entities
{
    public class KnowledgeBaseDocument
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = default!;
        public string StorageKey { get; set; } = default!;

        public string UploadedByUserId { get; set; }

        public KnowledgeBaseStatus Status { get; set; } = KnowledgeBaseStatus.Pending;
        public string? ErrorMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public ICollection<KnowledgeBaseChunk> Chunks { get; set; } = new List<KnowledgeBaseChunk>();
    }
}
