using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Admission.Domain.Entities
{
    public class Application
    {
        public Guid Id { get; set; }

        public string ApplicantUserId { get; set; }
        public Guid CycleId { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
        public DateTimeOffset? SubmittedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string? AdminNotes { get; set; }

        public Guid? AcceptedTrackBranchOfferingId { get; set; }        // New properties to track the final accepted offering
        public TrackBranchOffering? AcceptedTrackBranchOffering { get; set; }

        // Navigation properties
        public AdmissionCycle Cycle { get; set; } = null!;
        public ICollection<ApplicationPreference> Preferences { get; set; } = new List<ApplicationPreference>();
        public EligibilityResult? EligibilityResult { get; set; }
        public ICollection<SimulatedStageResult> SimulatedStageResults { get; set; } = new List<SimulatedStageResult>();
        public ICollection<ApplicationEnrollmentTask> EnrollmentTasks { get; set; } = new List<ApplicationEnrollmentTask>();
    }
}
