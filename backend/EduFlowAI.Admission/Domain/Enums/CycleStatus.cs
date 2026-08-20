namespace EduFlowAI.Admission.Domain.Enums
{
    // State machine statuses for an admission cycle
    public enum CycleStatus
    {
        None = 0,
        Draft = 1,
        Active = 2,
        Closed = 3
    }
}