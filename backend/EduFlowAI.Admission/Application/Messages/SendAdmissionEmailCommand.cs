namespace EduFlowAI.Admission.Application.Messages
{
    /// <summary>
    /// This record acts as the message payload that Wolverine will serialize 
    /// and push into the background queue (or RabbitMQ).
    /// </summary>
    /// <param name="Email"></param>
    /// <param name="Subject"></param>
    /// <param name="HtmlBody"></param>
    /// <param name="IdempotencyKey"></param>
    public sealed record SendAdmissionEmailCommand(
        string Email,
        string Subject,
        string HtmlBody,
        string IdempotencyKey
    );
}
