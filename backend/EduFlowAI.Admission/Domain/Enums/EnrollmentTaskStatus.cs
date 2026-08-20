namespace EduFlowAI.Admission.Domain.Enums
{
    /// <summary>
    /// Tracks the applicant's progress on a specific enrollment task.
    /// </summary>
    public enum EnrollmentTaskStatus
    {
        Pending = 1,
        UnderReview = 2,
        Completed = 3,
        Failed = 4
    }
}
