using EduFlowAI.Documents.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.DTOs
{
    public sealed record ReplacementDto(
        Guid Id,
        Guid DocumentId,
        string DocumentType,
        string Reason,
        ReplacementRequestStatus Status,
        DateTimeOffset RequestedAt
    );
}
