namespace EduFlowAI.Admission.Infrastructure.Options
{
    public sealed class N8nAdmissionEmailOptions
    {
        public const string SectionName = "N8nAdmissionEmail";

        public string WebhookUrl { get; set; } = string.Empty;

        public string WebhookSecret { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; } = 30;
    }
}
