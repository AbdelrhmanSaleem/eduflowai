using EduFlowAI.Identity.Domain.Enums;

namespace EduFlowAI.Identity.Domain.Entities;

public sealed class ApplicantProfile
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsProfileLocked { get; set; }

    public string FullNameEn { get; set; } = null!;
    public string FullNameAr { get; set; } = null!;
    public string NationalId { get; set; } = null!;
    public string Nationality { get; set; } = null!;
    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public CumulativeGrade CumulativeGrade { get; set; }
    public MilitaryStatus? MilitaryStatus { get; set; }

    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string University { get; set; } = null!;
    public string Faculty { get; set; } = null!;
    public string DegreeLevel { get; set; } = null!;
    public string Major { get; set; } = null!;
    public int GraduationYear { get; set; }

    public string UserId { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
