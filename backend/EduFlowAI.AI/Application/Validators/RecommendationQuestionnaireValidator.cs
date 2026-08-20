using EduFlowAI.AI.Application.DTOs;

namespace EduFlowAI.AI.Application.Validators;

public static class RecommendationQuestionnaireValidator
{
    private const int MaximumItemsPerField = 20;
    private const int MaximumTextLength = 500;

    public static IReadOnlyList<string> Validate(
        RecommendationQuestionnaireDto questionnaire)
    {
        ArgumentNullException.ThrowIfNull(questionnaire);

        var errors = new List<string>();

        ValidateOptionalText(
            questionnaire.Major,
            nameof(questionnaire.Major),
            errors);

        ValidateOptionalCollection(
            questionnaire.TechnicalCourses,
            nameof(questionnaire.TechnicalCourses),
            errors);

        ValidateOptionalCollection(
            questionnaire.Skills,
            nameof(questionnaire.Skills),
            errors);

        ValidateOptionalCollection(
            questionnaire.Interests,
            nameof(questionnaire.Interests),
            errors);

        ValidateOptionalCollection(
            questionnaire.PreferredActivities,
            nameof(questionnaire.PreferredActivities),
            errors);

        ValidateOptionalCollection(
            questionnaire.CareerGoals,
            nameof(questionnaire.CareerGoals),
            errors);

        ValidateOptionalText(
            questionnaire.AdditionalContext,
            nameof(questionnaire.AdditionalContext),
            errors);

        return errors;
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

        if (value.Length > MaximumTextLength)
        {
            errors.Add(
                $"{fieldName} must not exceed {MaximumTextLength} characters.");
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

        if (values.Any(value => value.Length > MaximumTextLength))
        {
            errors.Add(
                $"Each value in {fieldName} must not exceed " +
                $"{MaximumTextLength} characters.");
        }
    }
}