using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Domain.Enums
{
    public enum ApplicationStatus
    {
        None = 0,
        Draft = 1,
        DocumentsRequired = 2,
        EligibilityFailed = 3,
        UnderDocumentVerification = 4,
        NeedsHumanReview = 5,
        DocumentRejected = 6,
        AssessmentInProgress = 7,
        Admitted = 8,
        Waitlisted = 9,
        NotSelected = 10,
        Withdrawn = 11,
        Expired = 12
    }
}
