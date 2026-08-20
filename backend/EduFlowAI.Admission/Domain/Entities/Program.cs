namespace EduFlowAI.Admission.Domain.Entities
{
    // Represents a major scholarship or training program (e.g., 9-Month Professional Training)
    public sealed class Program
    {
        public Guid Id { get; set; }

        public Guid InstitutionId { get; set; }

        public string Name { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string Description { get; set; } = string.Empty;

        public int DurationMonths { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public Institution Institution { get; set; } = null!;
        public ICollection<Track> Tracks { get; set; } = new List<Track>();
        public ICollection<ProgramDocumentRequirement> DocumentRequirements { get; set; } = new List<ProgramDocumentRequirement>();
        public ICollection<AdmissionCycle> AdmissionCycles { get; set; } = new List<AdmissionCycle>();
    }
}
