namespace EduFlowAI.AI.Infrastructure.Options;

public class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    // How many chunks are handed to the answer prompt.
    public int MaxContextChunks { get; set; } = 10;

    // Slots reserved for literal keyword hits so a proper noun cannot be crowded out.
    public int MaxKeywordChunks { get; set; } = 4;
}
