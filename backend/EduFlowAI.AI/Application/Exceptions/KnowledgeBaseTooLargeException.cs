using System;

namespace EduFlowAI.AI.Application.Exceptions;

// The upload exceeded IngestionOptions.MaxUploadBytes — map to 413.
public class KnowledgeBaseTooLargeException : Exception
{
    public KnowledgeBaseTooLargeException(string message) : base(message)
    {
    }
}
