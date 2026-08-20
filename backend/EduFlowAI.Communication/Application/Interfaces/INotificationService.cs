using EduFlowAI.Communication.Domain.Enums;
using EduFlowAI.Shared.Kernel.Common;
using EduFlowAI.Shared.Kernel.Common.Pagination;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Communication.Application.Interfaces
{
    public interface INotificationService
    {
        Task NotifyAsync(CreateNotificationRequest request, CancellationToken cancellationToken);
        Task<Result<PaginatedResult<NotificationResponse>>> GetAllNotificationsByUserIdAsync(string userId, QueryParameters queryParameters, CancellationToken cancellationToken);
        Task<Result<bool>> MarkAsReadAsync(string userId, Guid notificationId, CancellationToken cancellationToken);
        Task<Result<int>> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken);
    }

    public sealed record CreateNotificationRequest(
        string UserId,
        Guid? ApplicationId,
        NotificationType Type,
        string Message);

    public sealed record NotificationResponse(
        Guid Id,
        string? Message,
        NotificationType Type,
        bool IsRead,
        DateTimeOffset CreatedAt);

}
