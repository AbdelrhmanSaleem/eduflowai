namespace EduFlowAI.Admission.Application.Models
{
    public sealed record AdmissionEmailNotification(
        string Email,
        string Subject,
        string HtmlBody,
        string IdempotencyKey
    );
}
