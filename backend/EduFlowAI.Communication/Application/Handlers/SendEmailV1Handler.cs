using EduFlowAI.Communication.Application.Interfaces.Emails;
using EduFlowAI.Communication.Domain.Enums;
using EduFlowAI.Shared.Messaging.Contracts.Communication.V1;
using Wolverine.Attributes;

namespace EduFlowAI.Communication.Application.Handlers
{
    public sealed class SendEmailV1Handler
    {
        private readonly IEmailContentBuilder _contentBuilder;
        private readonly IEmailSender _emailSender;

        public SendEmailV1Handler(IEmailContentBuilder contentBuilder, IEmailSender emailSender)
        {
            _contentBuilder = contentBuilder;
            _emailSender = emailSender;
        }

        [NonTransactional]
        public async Task Handle(SendEmailV1 message, CancellationToken cancellationToken)
        {
            var (subject, htmlBody) = _contentBuilder.Build((EmailType)message.EmailType, message.DataJson);
            await _emailSender.SendAsync(message.RecipientEmail, subject, htmlBody, cancellationToken);
        }
    }
}
