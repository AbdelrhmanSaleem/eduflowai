using EduFlowAI.Documents.Domain.Enums;
using EduFlowAI.Admission.Domain.Enums;

using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.DTOs
{
    public sealed record HumanReviewDto(
        Guid DocumentId,
        DocumentType DocumentType,
        string ApplicantName,
        string StorageKey,
        string OriginalFileName,
        DocumentStatus Status,
        string? VerificationDetailsJson
    );
}