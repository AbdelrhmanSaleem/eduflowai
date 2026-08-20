namespace EduFlowAI.AI.Infrastructure.Storage;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    // Empty uses a per-user path outside the build output; set an absolute path when hosting.
    public string Root { get; set; } = string.Empty;
}
