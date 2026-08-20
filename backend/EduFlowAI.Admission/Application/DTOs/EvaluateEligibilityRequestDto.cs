using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Application.DTOs
{
    /// <summary>
    /// DTO to securely receive the evaluation request from the front-end
    /// </summary>
    public class EvaluateEligibilityRequestDto
    {
        public Guid ApplicantId { get; set; }
        public Guid CycleId { get; set; }
        public Guid ApplicationId { get; set; }
    }
}
