using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.AI.Domain.Enums
{
    public enum KnowledgeBaseStatus
    {
        None = 0,
        Pending = 1,
        Indexing = 2,
        Indexed = 3,
        Failed = 4
    }
}
