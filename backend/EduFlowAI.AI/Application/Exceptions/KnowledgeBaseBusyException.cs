using System;

namespace EduFlowAI.AI.Application.Exceptions;

// A re-sync is already running and would race this operation — map to 409.
public class KnowledgeBaseBusyException : Exception
{
    public KnowledgeBaseBusyException(string message) : base(message)
    {
    }
}
