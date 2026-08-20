namespace EduFlowAI.Identity.Application.DTOs;

public enum IdentityFailureKind
{
    Validation,
    Unauthorized,
    Forbidden,
    Locked,
    Conflict,
    NotFound
}

public sealed record IdentityFailure(
    IdentityFailureKind Kind,
    string Title,
    string? Detail = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public sealed record IdentityOperationResult<T>(
    T? Value,
    IdentityFailure? Failure)
{
    public bool IsSuccess => Failure is null;

    public static IdentityOperationResult<T> Success(T? value = default) =>
        new(value, null);

    public static IdentityOperationResult<T> Fail(IdentityFailure failure) =>
        new(default, failure);
}
