using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Application.Models
{
    /// <summary>
    /// Represents a single failure reason to be serialized into FailureReasonsJson
    /// </summary>
    public class EligibilityFailureReason
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? ActualValue { get; set; }
        public object? AllowedValue { get; set; }
    }
}
