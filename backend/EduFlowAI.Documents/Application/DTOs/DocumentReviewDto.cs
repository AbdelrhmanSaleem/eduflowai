using EduFlowAI.Documents.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using EduFlowAI.Admission.Domain.Enums;


namespace EduFlowAI.Documents.Application.DTOs
{
    public sealed record DocumentReviewDto(
        Guid DocumentId,
        Guid ApplicationId,
        string ApplicantId,
        DocumentType DocumentType,
        string StorageKey,
        string OriginalFileName,
        DocumentStatus Status,
        string? VerificationDetailsJson
    );
}