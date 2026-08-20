namespace EduFlowAI.Admission.Application.DTOs
{
    /// <summary>
    /// DTO tailored specifically for the Applicant frontend to display 
    /// programs and their currently active admission cycles.
    /// </summary>
    /// <param name="CycleId">The ID of the Admission Cycle (used for creating drafts).</param>
    /// <param name="ProgramName">The name of the program.</param>
    /// <param name="ProgramCode">The code of the program.</param>
    /// <param name="CycleLabel">The label of the specific cycle (e.g., Intake 46).</param>
    /// <param name="StartDate">The start date of the cycle.</param>
    /// <param name="DeadlineUtc">The deadline of the cycle in UTC.</param>
    public record ActiveAdmissionCycleDto(
        Guid CycleId,
        string ProgramName,
        string ProgramCode,
        string ProgramDescription,
        string CycleLabel,
        DateOnly StartDate,
        DateTimeOffset DeadlineUtc
    );
}
