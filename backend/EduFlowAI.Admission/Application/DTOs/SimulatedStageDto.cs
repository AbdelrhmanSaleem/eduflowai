namespace EduFlowAI.Admission.Application.DTOs
{
    public record SimulatedStageDto
    {
        public Guid StageId { get; set; }
        public required string StageType { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; }
        public required string Result { get; set; }
        public string? TrackName { get; set; }

        // Date when the score was recorded
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
