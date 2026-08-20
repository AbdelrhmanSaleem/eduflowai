using EduFlowAI.Communication.Application.Interfaces.Emails;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using MailKit.Net.Smtp;
using System.Text;

namespace EduFlowAI.Communication.Infrastructure.ExternalServices
{
    public sealed record SmtpSettings(string Host, int Port, string Password, string FromAddress, string FromName);

    public sealed class EmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;
        public EmailSender(SmtpSettings settings) => _settings = settings;

        public async Task SendAsync(string recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
                message.To.Add(MailboxAddress.Parse(recipientEmail));
                message.Subject = subject;
                message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

                using var client = new SmtpClient
                {
                    CheckCertificateRevocation = false
                };
                await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, cancellationToken);
                await client.AuthenticateAsync(_settings.FromAddress, _settings.Password, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
            }
            catch (System.Net.Mail.SmtpException) { }
            catch (Exception) { }
        }
    }
}
