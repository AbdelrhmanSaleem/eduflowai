namespace EduFlowAI.AI.Application.Services;

using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Exceptions;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Application.Validators;
using EduFlowAI.AI.Infrastructure.ExternalServices.Gemini.DTOs;
using Microsoft.Extensions.Logging;

public sealed class TrackRecommendationService(
    IOfferedTrackReader offeredTrackReader,
    IRecommendationModelClient recommendationModelClient,
    ILogger<TrackRecommendationService> logger)
    : ITrackRecommendationService
{
    private const int MaximumRecommendations = 3;

    private const string AdvisoryNotice =
        "These recommendations are advisory only and do not determine eligibility or admission.";

    public async Task<TrackRecommendationResultDto> RecommendAsync(
        RecommendationQuestionnaireDto questionnaire,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(questionnaire);

        var validationErrors =
            RecommendationQuestionnaireValidator.Validate(questionnaire);

        if (validationErrors.Count > 0)
        {
            throw new RecommendationValidationException(validationErrors);
        }

        var offeredTracks =
            await offeredTrackReader.GetActiveOfferedTracksAsync(
                cancellationToken);

        if (offeredTracks.Count == 0)
        {
            return new TrackRecommendationResultDto
            {
                Recommendations = [],
                UsedFallback = false,
                AdvisoryNotice = AdvisoryNotice
            };
        }

        logger.LogInformation(
            "Active offered tracks sent to Gemini: {@Tracks}",
            offeredTracks.Select(track => new
            {
                track.TrackId,
                track.Name,
                track.Description,
                track.PrerequisiteTopics,
                track.Category,
                track.MinimumGrade,
                track.EligibilitySummary,
                track.TotalHours,
                track.GraduationYearLimitYears,
                track.Locations
            }));

        try
        {
            var modelResponse =
                await recommendationModelClient.RankAsync(
                    new RecommendationModelRequestDto
                    {
                        Questionnaire = questionnaire,
                        OfferedTracks = offeredTracks
                    },
                    cancellationToken);

            var validatedRecommendations =
                ValidateRecommendations(
                    modelResponse,
                    offeredTracks);

            if (validatedRecommendations.Count == 0)
            {
                logger.LogWarning(
                    "Gemini returned no valid track recommendations. Using fallback.");
            }

            if (validatedRecommendations.Count > 0)
            {
                return new TrackRecommendationResultDto
                {
                    Recommendations = validatedRecommendations,
                    UsedFallback = false,
                    AdvisoryNotice = AdvisoryNotice
                };
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Gemini track recommendation failed. Using fallback recommendations.");
        }

        return CreateFallback(offeredTracks);
    }

    private static IReadOnlyList<RecommendedTrackResultDto>
        ValidateRecommendations(
            RecommendationModelResponseDto? modelResponse,
            IReadOnlyList<OfferedTrackForRecommendationDto> offeredTracks)
    {
        if (modelResponse?.Recommendations is null)
        {
            return [];
        }

        var offeredTracksById = offeredTracks
            .GroupBy(track => track.TrackId)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        return modelResponse.Recommendations
            .Where(item =>
                item.TrackId != Guid.Empty &&
                item.Rank > 0 &&
                !string.IsNullOrWhiteSpace(item.Reason) &&
                offeredTracksById.ContainsKey(item.TrackId))
            .GroupBy(item => item.TrackId)
            .Select(group => group
                .OrderBy(item => item.Rank)
                .First())
            .OrderBy(item => item.Rank)
            .Take(MaximumRecommendations)
            .Select((item, index) =>
            {
                var track =
                    offeredTracksById[item.TrackId];

                return new RecommendedTrackResultDto
                {
                    TrackId = track.TrackId,
                    TrackName = track.Name,
                    Rank = index + 1,
                    Reason = item.Reason.Trim()
                };
            })
            .ToList();
    }

    private static TrackRecommendationResultDto CreateFallback(
        IReadOnlyList<OfferedTrackForRecommendationDto> offeredTracks)
    {
        var recommendations = offeredTracks
            .GroupBy(track => track.TrackId)
            .Select(group => group.First())
            .OrderBy(track => track.Name)
            .Take(MaximumRecommendations)
            .Select((track, index) =>
                new RecommendedTrackResultDto
                {
                    TrackId = track.TrackId,
                    TrackName = track.Name,
                    Rank = index + 1,
                    Reason =
                        "This track is currently offered. Review its description and prerequisites to assess your fit."
                })
            .ToList();

        return new TrackRecommendationResultDto
        {
            Recommendations = recommendations,
            UsedFallback = true,
            AdvisoryNotice = AdvisoryNotice
        };
    }
}
