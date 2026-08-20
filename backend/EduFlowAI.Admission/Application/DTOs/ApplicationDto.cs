namespace EduFlowAI.Admission.Application.DTOs
{
    public record ApplicationDto(
        Guid Id, string ApplicantUserId, Guid CycleId, string Status,
        List<PreferenceDto> Preferences
    );
}
