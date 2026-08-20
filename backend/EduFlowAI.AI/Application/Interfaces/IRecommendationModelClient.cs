using EduFlowAI.AI.Infrastructure.ExternalServices.Gemini.DTOs;

namespace EduFlowAI.AI.Application.Interfaces;

public interface IRecommendationModelClient
{
    Task<RecommendationModelResponseDto> RankAsync(
        RecommendationModelRequestDto request,
        CancellationToken cancellationToken = default);
}