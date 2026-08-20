using EduFlowAI.AI.Application.DTOs;

namespace EduFlowAI.AI.Application.Interfaces;

public interface IRecommendationUserContextService
{
    Task<RecommendationUserContextDto> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}