using EduFlowAI.AI.Application.DTOs;

namespace EduFlowAI.AI.Application.Interfaces;

public interface ITrackRecommendationService
{
    Task<TrackRecommendationResultDto> RecommendAsync(
        RecommendationQuestionnaireDto questionnaire,
        CancellationToken cancellationToken = default);
}