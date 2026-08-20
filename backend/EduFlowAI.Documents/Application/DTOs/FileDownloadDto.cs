using System.IO;

namespace EduFlowAI.Documents.Application.DTOs;

public sealed record FileDownloadDto(
    Stream Stream,
    string OriginalFileName
);