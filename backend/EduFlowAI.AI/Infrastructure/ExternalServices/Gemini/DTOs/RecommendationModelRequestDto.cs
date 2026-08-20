using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.AI.Application.DTOs;

namespace EduFlowAI.AI.Infrastructure.ExternalServices.Gemini.DTOs;

public sealed record RecommendationModelRequestDto
{
    public required RecommendationQuestionnaireDto Questionnaire { get; init; }

    public required IReadOnlyCollection<OfferedTrackForRecommendationDto>
        OfferedTracks
    { get; init; }
}