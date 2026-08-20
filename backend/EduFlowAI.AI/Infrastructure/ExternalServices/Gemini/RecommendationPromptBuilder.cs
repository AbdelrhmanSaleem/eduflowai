using System.Text.Json;
using EduFlowAI.AI.Infrastructure.ExternalServices.Gemini.DTOs;

namespace EduFlowAI.AI.Infrastructure.ExternalServices.Gemini;

internal static class RecommendationPromptBuilder
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public static string Build(RecommendationModelRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var input = new
        {
            questionnaire = request.Questionnaire,
            offeredTracks = request.OfferedTracks.Select(track => new
            {
                trackId = track.TrackId,
                name = track.Name,
                description = track.Description,
                prerequisiteTopics = track.PrerequisiteTopics,
                category = track.Category,
                minimumGrade = track.MinimumGrade,
                eligibilitySummary = track.EligibilitySummary,
                totalHours = track.TotalHours,
                graduationYearLimitYears = track.GraduationYearLimitYears,
                locations = track.Locations
            })
        };

        return $$"""
            You are an advisory track recommendation assistant for the ITI admission platform.

            Use only the supplied questionnaire and offered track metadata.

            Rules:
            - Return at most three distinct recommendations.
            - Use only the supplied trackId values.
            - Rank results from 1 upward.
            - Prefer tracks whose category, description, prerequisiteTopics, and
              eligibilitySummary best match the user's stated major, skills, interests,
              and career goals.
            - Give one concise, track-specific reason for each recommendation.
            - Each reason must explicitly connect at least one questionnaire signal
              (major, skills, interests, or career goals)
              to the selected track's name, category, description, prerequisiteTopics,
              or eligibilitySummary.
            - Do not use generic reasons such as "this track is currently offered".
            - Do not invent tracks, IDs, prerequisites, scores, or percentages.
            - Recommendations are advisory and do not determine eligibility or admission.
            - Return JSON matching the requested schema exactly.

            Input:
            {{JsonSerializer.Serialize(input, JsonOptions)}}
            """;
    }
}
