using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EduFlowAI.AI.Domain.Entities
{
    public class KnowledgeBaseChunk
    {
        public Guid Id { get; set; }

        public Guid KnowledgeBaseDocumentId { get; set; }

        public string Content { get; set; } = default!;

        public Pgvector.Vector Embedding { get; set; } = default!;

        public KnowledgeBaseDocument Document { get; set; } = null!;
    }
}
