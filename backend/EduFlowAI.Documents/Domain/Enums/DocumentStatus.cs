using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Domain.Enums
{
    public enum DocumentStatus
    {
        None = 0,
        Uploaded = 1,
        Verifying = 2,
        Approved = 3,
        NeedsHumanReview = 4,
        ReplacementRequested = 5,
        Rejected = 6
    }
}
