using System.ComponentModel.DataAnnotations;

namespace EduFlowAI.Identity.Application.DTOs;

public sealed class CreateOperationsManagerRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    [Required, RegularExpression("^(en|ar)$")]
    public string PreferredLanguage { get; init; } = "en";
}

public sealed record OperationsManagerResponse(
    string UserId,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeactivatedAt);
