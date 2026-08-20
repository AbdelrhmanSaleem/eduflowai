using EduFlowAI.AI.Application.Exceptions;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Services;
using EduFlowAI.AI.Presentation.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace EduFlowAI.Api.Controllers;

[ApiController]
[Route("api/tracks/recommend")]
public sealed class TrackRecommendationsController(
    ITrackRecommendationService recommendationService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RecommendTracksResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecommendTracksResponse>> Recommend(
        [FromBody] RecommendTracksRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var questionnaire = new RecommendationQuestionnaireDto
            {
                Major = request.Major,
                TechnicalCourses = request.TechnicalCourses,
                Skills = request.Skills,
                Interests = request.Interests,
                PreferredActivities = request.PreferredActivities,
                CareerGoals = request.CareerGoals,
                AdditionalContext = request.AdditionalContext
            };

            var result = await recommendationService.RecommendAsync(
                questionnaire,
                cancellationToken);

            return Ok(new RecommendTracksResponse
            {
                Recommendations = result.Recommendations
                    .Select(item => new RecommendedTrackResponse
                    {
                        TrackId = item.TrackId,
                        TrackName = item.TrackName,
                        Rank = item.Rank,
                        Reason = item.Reason
                    })
                    .ToList(),

                UsedFallback = result.UsedFallback,
                AdvisoryNotice = result.AdvisoryNotice
            });
        }
        catch (RecommendationValidationException exception)
        {
            var errors = exception.Errors
                .Select((error, index) => new
                {
                    Key = $"questionnaire[{index}]",
                    Value = new[] { error }
                })
                .ToDictionary(item => item.Key, item => item.Value);

            foreach (var error in exception.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return ValidationProblem(ModelState);
        }
    }
}