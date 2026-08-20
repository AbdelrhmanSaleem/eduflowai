using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Admission.Domain.Entities
{
    /// <summary>
    /// The master table containing the static templates for all enrollment tasks.
    /// </summary>
    public class EnrollmentTaskLookup
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public EnrollmentTaskType TaskType { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
