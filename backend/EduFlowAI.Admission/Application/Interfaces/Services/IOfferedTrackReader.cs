
using EduFlowAI.Admission.Application.DTOs;

namespace EduFlowAI.Admission.Application.Interfaces.Services;
public interface IOfferedTrackReader
{
    Task<IReadOnlyList<OfferedTrackForRecommendationDto>>
        GetActiveOfferedTracksAsync(
            CancellationToken cancellationToken = default);
}