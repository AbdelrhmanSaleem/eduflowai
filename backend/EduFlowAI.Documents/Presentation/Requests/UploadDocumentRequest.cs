using Microsoft.AspNetCore.Http;
using EduFlowAI.Documents.Domain.Enums;
using EduFlowAI.Admission.Domain.Enums;


namespace EduFlowAI.Documents.Presentation.Requests;

public sealed record UploadDocumentRequest(
    DocumentType DocumentType,
    IFormFile File
);