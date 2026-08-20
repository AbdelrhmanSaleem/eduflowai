using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Application.Messages;
using EduFlowAI.Admission.Application.Models;
using Microsoft.Extensions.Logging;

namespace EduFlowAI.Admission.Application.Handlers
{
    /// <summary>
    /// Wolverine automatically discovers this handler during startup.
    /// It listens for 'SendAdmissionEmailCommand' messages in the background queue.
    /// </summary>
    public sealed class SendAdmissionEmailHandler
    {
        private readonly IAdmissionEmailNotificationService _emailNotificationService;
        private readonly ILogger<SendAdmissionEmailHandler> _logger;

        public SendAdmissionEmailHandler(
            IAdmissionEmailNotificationService emailNotificationService,
            ILogger<SendAdmissionEmailHandler> logger)
        {
            _emailNotificationService = emailNotificationService;
            _logger = logger;
        }

        // This method is executed in the background by Wolverine's worker.
        // It does not block the main HTTP request that triggered the allocation.
        public async Task HandleAsync(SendAdmissionEmailCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Wolverine background worker is processing email for: {Email} using IdempotencyKey: {Key}",
                command.Email,
                command.IdempotencyKey);

            // 1. Map the queued command to the domain notification model
            var notification = new AdmissionEmailNotification(
                command.Email,
                command.Subject,
                command.HtmlBody,
                command.IdempotencyKey
            );

            // 2. Execute the HTTP call to the n8n webhook
            // Note: If the n8n API is down or times out, SendAsync will throw an HttpRequestException.
            // Wolverine will catch this exception and automatically retry the message later 
            // or move it to a Dead Letter Queue (DLQ) based on your configuration.
            await _emailNotificationService.SendAsync(notification, cancellationToken);
        }
    }
}