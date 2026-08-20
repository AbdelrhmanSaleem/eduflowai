using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json.Serialization;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Application.Models;
using EduFlowAI.Admission.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduFlowAI.Admission.Infrastructure.ExternalServices
{
    public sealed class N8nAdmissionEmailService
    : IAdmissionEmailNotificationService
    {
        private const string WebhookSecretHeader = "X-Webhook-Secret";

        private readonly HttpClient _httpClient;
        private readonly N8nAdmissionEmailOptions _options;
        private readonly ILogger<N8nAdmissionEmailService> _logger;

        public N8nAdmissionEmailService(
            HttpClient httpClient,
            IOptions<N8nAdmissionEmailOptions> options,
            ILogger<N8nAdmissionEmailService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(AdmissionEmailNotification notification,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(notification);

            ValidateNotification(notification);

            var payload = new N8nEmailRequest(
                notification.Email,
                notification.Subject,
                notification.HtmlBody,
                notification.IdempotencyKey);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                _options.WebhookUrl);

            request.Headers.Add(
                WebhookSecretHeader,
                _options.WebhookSecret);

            request.Content = JsonContent.Create(payload);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "The n8n admission email webhook rejected message {IdempotencyKey} with status code {StatusCode}.",
                    notification.IdempotencyKey,
                    (int)response.StatusCode);

                throw new HttpRequestException(
                    $"The n8n email webhook returned HTTP {(int)response.StatusCode}.",
                    inner: null,
                    response.StatusCode);
            }

            var result = await response.Content
                .ReadFromJsonAsync<N8nEmailResponse>(
                    cancellationToken: cancellationToken);

            if (result is null)
            {
                throw new InvalidOperationException(
                    "The n8n email webhook returned an empty response.");
            }

            if (!result.Success)
            {
                throw new InvalidOperationException(
                    "The n8n email webhook did not confirm successful delivery.");
            }

            if (!string.Equals(
                    result.IdempotencyKey,
                    notification.IdempotencyKey,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The n8n email webhook returned an unexpected idempotency key.");
            }

            _logger.LogInformation(
                "The n8n admission email webhook accepted message {IdempotencyKey}.",
                notification.IdempotencyKey);
        }

        private static void ValidateNotification(
            AdmissionEmailNotification notification)
        {
            if (!MailAddress.TryCreate(notification.Email, out _))
            {
                throw new ArgumentException(
                    "A valid recipient email address is required.",
                    nameof(notification));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(
                notification.Subject);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                notification.HtmlBody);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                notification.IdempotencyKey);
        }

        private sealed record N8nEmailRequest(
            [property: JsonPropertyName("email")]
        string Email,

            [property: JsonPropertyName("subject")]
        string Subject,

            [property: JsonPropertyName("htmlBody")]
        string HtmlBody,

            [property: JsonPropertyName("idempotencyKey")]
        string IdempotencyKey
        );

        private sealed record N8nEmailResponse(
            [property: JsonPropertyName("success")]
        bool Success,

            [property: JsonPropertyName("idempotencyKey")]
        string? IdempotencyKey
        );
    }
}

