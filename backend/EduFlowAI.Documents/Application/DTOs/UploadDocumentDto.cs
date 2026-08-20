using System;
using Microsoft.AspNetCore.Http;
using EduFlowAI.Documents.Domain.Enums;
using EduFlowAI.Admission.Domain.Enums;


namespace EduFlowAI.Documents.Application.DTOs
{
    public sealed record UploadDocumentDto(
        Guid ApplicationId,
        DocumentType DocumentType,
        IFormFile File
    );
}