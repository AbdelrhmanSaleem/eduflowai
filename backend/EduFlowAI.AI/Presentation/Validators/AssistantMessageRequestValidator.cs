using EduFlowAI.AI.Presentation.DTOs;

namespace EduFlowAI.AI.Presentation.Validators;

public static class AssistantMessageRequestValidator
{
    private const int MaximumMessageLength = 2000;
    private const int MaximumRecommendationTextLength = 500;
    private const int MaximumItemsPerField = 20;

    private static readonly string[] SupportedLanguages =
    [
        "en",
        "ar"
    ];

    public static IReadOnlyList<string> Validate(
        AssistantMessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<string>();

        ValidateMessage(request.Message, errors);
        ValidateLanguage(request.Language, errors);

        if (request.Recommendation is not null)
        {
            ValidateRecommendationProgress(
                request.Recommendation,
                errors);
        }

        return errors;
    }

    private static void ValidateMessage(
        string? message,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            errors.Add(
                $"{nameof(AssistantMessageRequest.Message)} is required.");

            return;
        }

        if (message.Length > MaximumMessageLength)
        {
            errors.Add(
                $"{nameof(AssistantMessageRequest.Message)} must not exceed " +
                $"{MaximumMessageLength} characters.");
        }
    }

    private static void ValidateLanguage(
        string? language,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return;
        }

        if (!SupportedLanguages.Contains(
                language,
                StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(
                $"{nameof(AssistantMessageRequest.Language)} must be either " +
                "'en' or 'ar'.");
        }
    }

    private static void ValidateRecommendationProgress(
        RecommendationQuestionnaireProgressDto recommendation,
        ICollection<string> errors)
    {
        ValidateOptionalText(
            recommendation.Major,
            nameof(recommendation.Major),
            errors);

        ValidateOptionalCollection(
            recommendation.TechnicalCourses,
            nameof(recommendation.TechnicalCourses),
            errors);

        ValidateOptionalCollection(
            recommendation.Skills,
            nameof(recommendation.Skills),
            errors);

        ValidateOptionalCollection(
            recommendation.Interests,
            nameof(recommendation.Interests),
            errors);

        ValidateOptionalCollection(
            recommendation.PreferredActivities,
            nameof(recommendation.PreferredActivities),
            errors);

        ValidateOptionalCollection(
            recommendation.CareerGoals,
            nameof(recommendation.CareerGoals),
            errors);

        ValidateOptionalText(
            recommendation.AdditionalContext,
            nameof(recommendation.AdditionalContext),
            errors);
    }

    private static void ValidateOptionalText(
        string? value,
        string fieldName,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Length > MaximumRecommendationTextLength)
        {
            errors.Add(
                $"{fieldName} must not exceed " +
                $"{MaximumRecommendationTextLength} characters.");
        }
    }

    private static void ValidateOptionalCollection(
        IReadOnlyCollection<string>? values,
        string fieldName,
        ICollection<string> errors)
    {
        if (values is null)
        {
            return;
        }

        if (values.Count > MaximumItemsPerField)
        {
            errors.Add(
                $"{fieldName} must not contain more than " +
                $"{MaximumItemsPerField} values.");
        }

        if (values.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add(
                $"{fieldName} must not contain empty values.");
        }

        if (values.Any(
                value => value.Length >
                         MaximumRecommendationTextLength))
        {
            errors.Add(
                $"Each value in {fieldName} must not exceed " +
                $"{MaximumRecommendationTextLength} characters.");
        }
    }
}