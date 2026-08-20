namespace EduFlowAI.Admission.Application.DTOs
{
    /// <summary>
    /// Used temporarily in memory during the allocation process to track applicant scores per preference.
    /// </summary>
    public class ApplicantTrackScoreDto
    {
        public Guid ApplicationId { get; set; }
        public Guid TrackBranchOfferingId { get; set; }
        public short PreferenceRank { get; set; }

        // Calculated based on the weights you will provide
        public decimal TotalWeightedScore { get; set; }

        // Stored explicitly to be used as Tie-Breakers based on SelectionStage
        public decimal TechnicalExamScore { get; set; }
        public decimal TechnicalInterviewScore { get; set; }

        // Tracks whether the applicant passed all required stages for this specific track
        public bool IsEligibleForTrack { get; set; }
    }
}
