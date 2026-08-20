namespace EduFlowAI.Documents.Infrastructure.Storage;

public class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    /// <summary>
    /// S3 bucket name used for secure document storage.
    /// Both the API and Worker must point to the same bucket.
    /// </summary>
    public string BucketName { get; set; } = "eduflowai-document-storage";
    public string Root { get; set; } = @"D:\EduFlowAI\SecureStorage";
}
