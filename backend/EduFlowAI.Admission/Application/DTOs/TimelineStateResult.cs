using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Application.DTOs
{
    /// <summary>
    /// A dedicated record to hold the calculated timeline state
    /// </summary>
    public record TimelineStateResult 
    {
        public int Percentage { get; init; }
        public string AppStatus { get; init; } = "Pending";
        public string EligStatus { get; init; } = "Pending";
        public string VerifStatus { get; init; } = "Pending";
        public string EngStatus { get; init; } = "Pending";
        public string TechStatus { get; init; } = "Pending";
        public string IntStatus { get; init; } = "Pending";
        public string FinalStatus { get; init; } = "Pending";
    }
}
