using EduFlowAI.AI.Presentation.DTOs;

namespace EduFlowAI.AI.Presentation.Interfaces;

public interface IAssistantMessageService
{
    Task<AssistantResponse> HandleAsync(
        AssistantMessageRequest request,
        Guid userId,
        bool isAuthenticated,
        CancellationToken cancellationToken = default);
}