using EduFlowAI.Communication.Infrastructure.Hubs;
using EduFlowAI.Shared.Messaging.Contracts.Communication.V1;
using Microsoft.AspNetCore.SignalR;
using Wolverine.Attributes;

namespace EduFlowAI.Api.Messaging
{
    public sealed class NotificationCreatedV1Handler
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationCreatedV1Handler(IHubContext<NotificationHub> hubContext) => _hubContext = hubContext;

        [NonTransactional]
        public async Task Handle(NotificationCreatedV1 message, CancellationToken cancellationToken)
        {
            await _hubContext.Clients.User(message.UserId).SendAsync(
                "notification-created",
                new { message.NotificationId, message.ApplicationId, message.NotificationType, message.Message, message.CreatedAtUtc },
                cancellationToken);
        }
    }
}
