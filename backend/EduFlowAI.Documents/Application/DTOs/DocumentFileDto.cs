using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.DTOs
{
    public sealed record DocumentFileDto(
        Stream Content,
        string ContentType,
        string FileName
    );
}
