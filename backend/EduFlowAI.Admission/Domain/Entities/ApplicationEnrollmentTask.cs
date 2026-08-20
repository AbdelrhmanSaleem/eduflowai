using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Admission.Domain.Entities
{
    /// <summary>
    /// The transactional table linking a specific application to a task and tracking its status.
    /// </summary>
    public class ApplicationEnrollmentTask
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid TaskId { get; set; }

        public EnrollmentTaskStatus Status { get; set; } = EnrollmentTaskStatus.Pending;
        public string? Message { get; set; }
        public string? ActionUrl { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public Application Application { get; set; } = null!;
        public EnrollmentTaskLookup TaskLookup { get; set; } = null!;
    }
}
