using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Communication.Application.Interfaces.Emails
{
    public interface IEmailSender
    {
        Task SendAsync(string recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken);
    }
}
