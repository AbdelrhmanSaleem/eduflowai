using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EduFlowAI.Identity.Application.DTOs;

public sealed class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    [Required, RegularExpression("^(en|ar)$")]
    public string PreferredLanguage { get; init; } = "en";
}

public sealed record RegisterResponse(
    string UserId,
    string Email,
    bool RequiresEmailConfirmation,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? DevelopmentConfirmationToken = null);

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string Password { get; init; } = string.Empty;
}

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyCollection<string> Roles);

public sealed class ConfirmEmailRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Token { get; init; } = string.Empty;
}

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;
}

public sealed record ForgotPasswordResponse(
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? DevelopmentResetToken = null);

public sealed class ResetPasswordRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Token { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; init; } = string.Empty;
}
