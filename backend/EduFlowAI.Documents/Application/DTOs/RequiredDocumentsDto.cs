using EduFlowAI.Admission.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.DTOs
{
    public sealed record RequiredDocumentsDto(IReadOnlyList<DocumentType> DocumentTypes);
}
