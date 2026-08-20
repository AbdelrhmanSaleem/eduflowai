using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Admission.Domain.Entities
{
    public class SimulatedStageResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ApplicationId { get; set; }
        public Guid? TrackId { get; set; }

        public SelectionStage Stage { get; set; }
        public StageResult Result { get; set; } = StageResult.Pending;
        public decimal? Score { get; set; }

        // The maximum possible score for this assessment
        public decimal MaxScore { get; set; }

        // Audit fields
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public Application Application { get; set; } = null!;
        public Track? Track { get; set; }
    }
}
