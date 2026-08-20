using EduFlowAI.Communication.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Communication.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }

        public string UserId { get; set; }

        public Guid? ApplicationId { get; set; }

        public NotificationType Type { get; set; }
        public string Message { get; set; } = default!;
        public bool IsRead { get; set; }
        public GmailStatus GmailStatus { get; set; } = GmailStatus.NotRequested;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
