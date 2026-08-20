using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DTOs;

namespace EduFlowAI.AI.Application.Interfaces;

public interface IIntentRouter
{
    Task<IntentClassificationDto> ClassifyAsync(
        string userQuestion,
        List<ConversationTurnDto> context,
        CancellationToken cancellationToken = default);

    // What to show when RequiresClarification is true. "ar" or "en".
    string GetClarificationMessage(string language);
}