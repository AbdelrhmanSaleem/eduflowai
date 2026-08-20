namespace EduFlowAI.AI.Application.DTOs;

public class DetectedIntentDto
{
    public string Intent { get; set; } = string.Empty;  // knowledge, application_status, document_status, recommendation

    public decimal Confidence { get; set; }  // 0.0 to 1.0

    public string RoutedTo { get; set; } = string.Empty;
}
