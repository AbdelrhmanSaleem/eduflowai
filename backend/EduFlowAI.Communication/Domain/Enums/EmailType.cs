using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Communication.Domain.Enums
{
    public enum EmailType
    {
        ConfirmRegistration = 1,
        PasswordReset,
        ApplicationStatusChanged,
        DocumentReplacementRequested
    }
}
