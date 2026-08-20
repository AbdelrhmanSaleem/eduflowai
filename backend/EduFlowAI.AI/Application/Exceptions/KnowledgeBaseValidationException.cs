using System;

namespace EduFlowAI.AI.Application.Exceptions;

// The caller sent something unusable — map to 400.
public class KnowledgeBaseValidationException : Exception
{
    public KnowledgeBaseValidationException(string message) : base(message)
    {
    }
}
