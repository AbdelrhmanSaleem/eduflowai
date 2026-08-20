using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Shared.Messaging.Contracts.Documents.V1
{
    public enum DocumentVerificationOutcomeV1
    {
        ExactMatch = 1,
        ValidButDifferent = 2,
        MissingRequiredData = 3,
        UnreadableDocument = 4,
        InvalidDocumentType = 5
    }
}
