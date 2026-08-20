using EduFlowAI.Communication.Application.DbContextAbstraction;
using EduFlowAI.Communication.Application.Interfaces;
using EduFlowAI.Communication.Domain.Entities;
using EduFlowAI.Communication.Domain.Enums;
using EduFlowAI.Shared.Kernel.Common;
using EduFlowAI.Shared.Kernel.Common.Pagination;
using EduFlowAI.Shared.Kernel.Messaging;
using EduFlowAI.Shared.Messaging.Contracts.Communication.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.EntityFrameworkCore;

namespace EduFlowAI.Communication.Application.Services
{
    public sealed class NotificationService(ICommunicationDbContext _dbContext, IOutboxPublisher _outboxPublisher) 
        : INotificationService
    {
        public async Task<Result<PaginatedResult<NotificationResponse>>> GetAllNotificationsByUserIdAsync(string userId, QueryParameters queryParameters, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.Notifications.AsNoTracking()
                    .Where(x => x.UserId == userId).AsQueryable();

                if (!string.IsNullOrEmpty(queryParameters.Search))
                {
                    query = query.Where(n => n.Message.Contains(queryParameters.Search) 
                        || n.Type.ToString().Contains(queryParameters.Search));
                }
                if (!string.IsNullOrEmpty(queryParameters.Type))
                {
                    query = query.Where(n => n.Type.ToString() == queryParameters.Type);
                }
                else
                {
                    query = query.OrderByDescending(n => n.CreatedAt);
                }

                var totalCount = await query.CountAsync();
                var notifications = await query.Skip((queryParameters.Page - 1) * queryParameters.PageSize)
                    .Take(queryParameters.PageSize)
                    .Select(n => new NotificationResponse(
                        Id: n.Id,
                        Message: n.Message,
                        Type: n.Type,
                        IsRead: n.IsRead,
                        CreatedAt: n.CreatedAt
                    )).ToListAsync();

                var data = new PaginatedResult<NotificationResponse>
                {
                    Data = notifications,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)queryParameters.PageSize),
                    PageSize = queryParameters.PageSize,
                    TotalCount = totalCount
                };

                return Result<PaginatedResult<NotificationResponse>>.Success(data);
            }
            catch (Exception ex)
            {
                return Result<PaginatedResult<NotificationResponse>>.Failure(500, $"An error occurred while retrieving notifications: {ex.Message}");
            }
        }

        public async Task NotifyAsync(CreateNotificationRequest request, CancellationToken cancellationToken)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                ApplicationId = request.ApplicationId,
                Type = request.Type,
                Message = request.Message,
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Notifications.Add(notification);

            await _outboxPublisher.PublishAsync(new NotificationCreatedV1(
                MessageId: Guid.NewGuid(),
                CorrelationId: request.ApplicationId ?? notification.Id,
                CausationId: null,
                NotificationId: notification.Id,
                UserId: notification.UserId,
                ApplicationId: notification.ApplicationId,
                NotificationType: notification.Type.ToString(),
                Message: notification.Message,
                CreatedAtUtc: notification.CreatedAt,
                OccurredAtUtc: DateTimeOffset.UtcNow));

            //await _outboxPublisher.SaveChangesAndFlushMessagesAsync(cancellationToken);
        }

        public async Task<Result<bool>> MarkAsReadAsync(string userId, Guid notificationId, CancellationToken cancellationToken)
        {
            try
            {
                var updatedCount = await _dbContext.Notifications
                    .Where(notification =>
                        notification.Id == notificationId &&
                        notification.UserId == userId &&
                        !notification.IsRead)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            notification => notification.IsRead,
                            true),
                        cancellationToken);

                if (updatedCount > 0)
                {
                    return Result<bool>.Success(true);
                }

                var exists = await _dbContext.Notifications
                    .AnyAsync(
                        notification =>
                            notification.Id == notificationId &&
                            notification.UserId == userId,
                        cancellationToken);

                return exists
                    ? Result<bool>.Success(true)
                    : Result<bool>.Failure(
                        StatusCodes.Status404NotFound,
                        "Notification was not found.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    StatusCodes.Status500InternalServerError,
                    $"An error occurred while updating the notification: {ex.Message}");
            }
        }

        public async Task<Result<int>> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken)
        {
            try
            {
                var updatedCount = await _dbContext.Notifications
                    .Where(notification =>
                        notification.UserId == userId &&
                        !notification.IsRead)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            notification => notification.IsRead,
                            true),
                        cancellationToken);

                return Result<int>.Success(updatedCount);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(
                    StatusCodes.Status500InternalServerError,
                    $"An error occurred while updating notifications: {ex.Message}");
            }
        }
    }
}
