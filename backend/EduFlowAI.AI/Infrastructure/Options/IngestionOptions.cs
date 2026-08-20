namespace EduFlowAI.AI.Infrastructure.Options;

public class IngestionOptions
{
    public const string SectionName = "Ingestion";

    // Transcribe with the model instead of local iText extraction.
    public bool UseAiExtraction { get; set; } = true;

    // Base64 inflates the payload by roughly a third, so stay well under the request limit.
    public int MaxInlineBytesForAi { get; set; } = 8 * 1024 * 1024;

    public long MaxUploadBytes { get; set; } = 20L * 1024 * 1024;

    // In-memory queue: a restart strands Pending/Indexing docs. Set false in a second host sharing
    // this module, else both would index the same documents.
    public bool RequeueStrandedOnStartup { get; set; } = true;

    public int MaxChunkChars { get; set; } = 1200;

    public int ChunkOverlapChars { get; set; } = 200;
}
