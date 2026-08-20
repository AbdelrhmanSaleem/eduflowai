using EduFlowAI.Identity.Application.DTOs;
using Microsoft.AspNetCore.Identity;

namespace EduFlowAI.Identity.Application.Services;

internal static class IdentityResultFactory
{
    public static IdentityOperationResult<T> ValidationFailure<T>(
        IdentityResult result) =>
        IdentityOperationResult<T>.Fail(new IdentityFailure(
            IdentityFailureKind.Validation,
            "Identity validation failed",
            Errors: result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error => error.Description)
                        .ToArray())));

    public static IdentityOperationResult<T> ValidationFailure<T>(
        string key,
        string error,
        string title = "Validation failed") =>
        IdentityOperationResult<T>.Fail(new IdentityFailure(
            IdentityFailureKind.Validation,
            title,
            Errors: new Dictionary<string, string[]>
            {
                [key] = [error]
            }));
}
