using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Communication.Domain.Enums
{
    public enum NotificationType
    {
        None = 0,
        DocumentsRequired = 1,
        DocumentReplacementRequested = 2,
        DocumentApproved = 3,
        DocumentRejected = 4,
        ApplicationStatusChanged = 5
    }
}
