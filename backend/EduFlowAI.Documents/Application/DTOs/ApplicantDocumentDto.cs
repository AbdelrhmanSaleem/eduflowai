using System;
using EduFlowAI.Documents.Domain.Enums;
using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Documents.Application.DTOs;

public sealed record ApplicantDocumentDto(
    Guid Id,
    DocumentType DocumentType,
    string OriginalFileName,
    DocumentStatus Status,
    DateTimeOffset CreatedAt
);