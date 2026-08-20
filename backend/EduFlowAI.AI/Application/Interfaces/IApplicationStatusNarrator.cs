using System;
using System.Threading;
using System.Threading.Tasks;

namespace EduFlowAI.AI.Application.Interfaces;

// Phrases the application status warmly; the status is a fact the model never decides or invents.
public interface IApplicationStatusNarrator
{
    Task<string> NarrateAsync(
        string currentStatus,
        DateTimeOffset lastUpdatedAt,
        string language,
        bool isContinuingConversation = false,
        CancellationToken cancellationToken = default);
}
