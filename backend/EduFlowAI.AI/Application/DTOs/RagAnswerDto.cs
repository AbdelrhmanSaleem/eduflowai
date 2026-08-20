using System.Collections.Generic;

namespace EduFlowAI.AI.Application.DTOs;

public class RagAnswerDto
{
    public string Answer { get; set; } = string.Empty;

    public List<string> Sources { get; set; } = new();
}
