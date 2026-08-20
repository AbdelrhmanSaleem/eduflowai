using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EduFlowAI.Identity.Domain.Enums;

namespace EduFlowAI.Identity.Application.DTOs;

public sealed record UpdateProfileRequest
{
    [Required, MaxLength(300)]
    public string FullNameEn { get; init; } = string.Empty;

    [Required, MaxLength(300)]
    public string FullNameAr { get; init; } = string.Empty;

    [Required, RegularExpression("^[0-9]{14}$")]
    public string NationalId { get; init; } = string.Empty;

    [Required, MaxLength(5)]
    public string Nationality { get; init; } = string.Empty;

    public DateOnly DateOfBirth { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Gender Gender { get; init; }

    [MaxLength(2000)]
    public string? Address { get; init; }

    [MaxLength(100)]
    public string? Governorate { get; init; }

    [Required, MaxLength(200)]
    public string University { get; init; } = string.Empty;

    [Required, MaxLength(200)]
    public string Faculty { get; init; } = string.Empty;

    [Required, MaxLength(30)]
    public string DegreeLevel { get; init; } = string.Empty;

    [Required, MaxLength(200)]
    public string Major { get; init; } = string.Empty;

    public int GraduationYear { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CumulativeGrade CumulativeGrade { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MilitaryStatus? MilitaryStatus { get; init; }

    [Phone, MaxLength(50)]
    public string? PhoneNumber { get; init; }

    [Required, RegularExpression("^(en|ar)$")]
    public string PreferredLanguage { get; init; } = "en";

    public bool GmailNotificationsEnabled { get; init; }
}

public sealed record ProfileResponse(
    string UserId,
    string Email,
    string? PhoneNumber,
    string PreferredLanguage,
    bool GmailNotificationsEnabled,
    bool IsComplete,
    bool IsProfileLocked,
    string? FullNameEn,
    string? FullNameAr,
    string? NationalId,
    string? Nationality,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address,
    string? Governorate,
    string? University,
    string? Faculty,
    string? DegreeLevel,
    string? Major,
    int? GraduationYear,
    string? CumulativeGrade,
    string? MilitaryStatus,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt);
