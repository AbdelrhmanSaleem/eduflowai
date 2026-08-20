using EduFlowAI.Communication.Domain.Enums;
using EduFlowAI.Shared.Kernel.Messaging;
using EduFlowAI.Shared.Messaging.Contracts.Communication.V1;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Communication.Application.Interfaces.Emails
{
    public interface IEmailDispatcher
    {
        ValueTask RequestEmailAsync(EmailType type, string recipientEmail, object? data, CancellationToken cancellationToken);
    }

    public sealed class EmailDispatcher : IEmailDispatcher
    {
        private readonly IOutboxPublisher _outboxPublisher;
        public EmailDispatcher(IOutboxPublisher outboxPublisher) => _outboxPublisher = outboxPublisher;

        public ValueTask RequestEmailAsync(EmailType type, string recipientEmail, object? data, CancellationToken cancellationToken)
            => _outboxPublisher.PublishAsync(new SendEmailV1(
                Guid.NewGuid(), Guid.NewGuid(), null,
                (int)type, recipientEmail,
                System.Text.Json.JsonSerializer.Serialize(data!),
                DateTimeOffset.UtcNow));
    }
}
