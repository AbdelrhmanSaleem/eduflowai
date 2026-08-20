namespace EduFlowAI.Admission.Application.DTOs
{
    /// <summary>
    /// DTO representing an applicant's track and branch preference.
    /// </summary>
    public record PreferenceDto(
        Guid TrackId,
        Guid BranchId,
        short Rank // 1 for Primary, 2 for Backup
    );
}
