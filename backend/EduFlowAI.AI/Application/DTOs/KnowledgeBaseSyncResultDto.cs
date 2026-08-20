namespace EduFlowAI.AI.Application.DTOs;

public class KnowledgeBaseSyncResultDto
{
    public int TotalDocuments { get; set; }
    public int Indexed { get; set; }
    public int Failed { get; set; }
}
