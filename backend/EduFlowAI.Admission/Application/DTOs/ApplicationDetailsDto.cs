namespace EduFlowAI.Admission.Application.DTOs
{
    /// <summary>
    /// DTO for returning full application details to the frontend for the Edit Page.
    /// </summary>
    public record ApplicationDetailsDto(
        Guid Id,
        string ApplicantUserId,
        Guid CycleId,
        string CycleName,
        DateTimeOffset CycleDeadlineUtc,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        List<PreferenceDto> Preferences
    );
}
