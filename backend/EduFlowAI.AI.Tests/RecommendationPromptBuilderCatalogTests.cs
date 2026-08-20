using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Infrastructure.ExternalServices.Gemini;
using EduFlowAI.AI.Infrastructure.ExternalServices.Gemini.DTOs;

namespace EduFlowAI.AI.Tests;

public sealed class RecommendationPromptBuilderCatalogTests
{
    [Fact]
    public void Build_includes_official_catalog_matching_and_eligibility_metadata()
    {
        Guid trackId = Guid.Parse("40000000-0000-0000-0000-000000000012");
        var request = new RecommendationModelRequestDto
        {
            Questionnaire = new RecommendationQuestionnaireDto
            {
                Major = "Mechatronics",
                Skills = ["PLC programming"],
                CareerGoals = ["Industrial automation engineer"]
            },
            OfferedTracks =
            [
                new OfferedTrackForRecommendationDto
                {
                    TrackId = trackId,
                    Name = "Industrial Automation",
                    Description = "Designs and troubleshoots automation systems.",
                    PrerequisiteTopics = ["Control systems", "PLCs"],
                    Category = "Industrial Systems",
                    MinimumGrade = "Good",
                    EligibilitySummary =
                        "Minimum graduation grade: Good. Graduation must be within the last 5 years.",
                    TotalHours = null,
                    GraduationYearLimitYears = 5,
                    Locations = ["Smart Village"]
                }
            ]
        };

        string prompt = RecommendationPromptBuilder.Build(request);

        Assert.Contains("category, description, prerequisiteTopics", prompt);
        Assert.Contains($"\"trackId\":\"{trackId}\"", prompt);
        Assert.Contains("\"category\":\"Industrial Systems\"", prompt);
        Assert.Contains("\"minimumGrade\":\"Good\"", prompt);
        Assert.Contains("\"graduationYearLimitYears\":5", prompt);
        Assert.Contains("\"totalHours\":null", prompt);
        Assert.Contains("\"locations\":[\"Smart Village\"]", prompt);
        Assert.Contains("Graduation must be within the last 5 years", prompt);
    }
}
