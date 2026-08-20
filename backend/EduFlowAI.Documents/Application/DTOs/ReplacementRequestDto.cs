using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.DTOs
{
    public class ReplacementRequestDto
    {
        public Guid DocumentId { get; set; }
        public string ApplicantId { get; set; }
        public string Reason { get; set; }
    }
}
