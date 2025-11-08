using Application.Models;
using Domain.Models.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Application.Services.Email
{
    public class MailKitEmailProvider : IEmailProvider
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<MailKitEmailProvider> _logger;

        public MailKitEmailProvider(IOptions<EmailSettings> settings, ILogger<MailKitEmailProvider> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<EmailResult> SendAsync(string toEmail, EmailContent content)
        {
            return await SendAsync(toEmail, null, content);
        }

        public async Task<EmailResult> SendAsync(string toEmail, string toName, EmailContent content)
        {
            try
            {
                var mimeMessage = CreateMimeMessage(toEmail, toName, content);

                using var client = new SmtpClient();

                // Configure security options based on settings
                var secureSocketOptions = _settings.UseStartTls
                    ? SecureSocketOptions.StartTls
                    : (_settings.EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);

                await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, secureSocketOptions);

                // Only authenticate if username/password are provided
                if (!string.IsNullOrEmpty(_settings.SmtpUsername) && !string.IsNullOrEmpty(_settings.SmtpPassword))
                {
                    await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);
                }

                var messageId = await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}. MessageId: {MessageId}", toEmail, messageId);
                return EmailResult.Success(messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}. SMTP: {SmtpServer}:{SmtpPort}",
                    toEmail, _settings.SmtpServer, _settings.SmtpPort);
                return EmailResult.Failure(ex.Message);
            }
        }

        public async Task<List<EmailResult>> SendBulkAsync(List<(string Email, string Name)> recipients, EmailContent content)
        {
            var results = new List<EmailResult>();

            // For production, consider using a more efficient bulk sending approach
            // or a service like SendGrid, Amazon SES, etc.
            foreach (var recipient in recipients)
            {
                var result = await SendAsync(recipient.Email, recipient.Name, content);
                results.Add(result);

                // Add small delay to avoid overwhelming SMTP server
                await Task.Delay(100);
            }

            return results;
        }

        private MimeMessage CreateMimeMessage(string toEmail, string toName, EmailContent content)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(string.IsNullOrEmpty(toName)
                ? MailboxAddress.Parse(toEmail)
                : new MailboxAddress(toName, toEmail));

            message.Subject = content.Subject;

            var bodyBuilder = new BodyBuilder();

            if (!string.IsNullOrEmpty(content.HtmlBody))
                bodyBuilder.HtmlBody = content.HtmlBody;

            if (!string.IsNullOrEmpty(content.PlainTextBody))
                bodyBuilder.TextBody = content.PlainTextBody;

            // Add attachments if any
            foreach (var attachment in content.Attachments)
            {
                if (File.Exists(attachment))
                    bodyBuilder.Attachments.Add(attachment);
            }

            message.Body = bodyBuilder.ToMessageBody();
            return message;
        }
    }
}
