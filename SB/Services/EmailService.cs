using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SB.Interfaces;
using System.Net.Mail;

namespace SB.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sends an email asynchronously using SMTP via MailKit.
        /// </summary>
        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));

            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Email subject is required.", nameof(subject));

            if (string.IsNullOrWhiteSpace(htmlBody))
                throw new ArgumentException("Email body is required.", nameof(htmlBody));

            try
            {
                var message = new MimeMessage();
                var fromEmail = _configuration["Email:FromEmail"] ?? "smpickettt@gmail.com";
                var fromName = _configuration["Email:FromName"] ?? "Smart Bracelet";

                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

                var smtpHost = _configuration["Email:Smtp:Host"] ?? "localhost";
                var smtpPort = int.TryParse(_configuration["Email:Smtp:Port"], out var port) ? port : 25;
                var smtpUser = _configuration["Email:Smtp:User"];
                var smtpPass = _configuration["Email:Smtp:Pass"];
                var useSsl = bool.TryParse(_configuration["Email:Smtp:UseSsl"], out var ssl) && ssl;
                var timeoutMs = int.TryParse(_configuration["Email:Smtp:TimeoutMs"], out var timeout) ? timeout : 30000;

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.Timeout = timeoutMs;

                    // Connect with SSL/TLS handling
                    if (useSsl)
                    {
                        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);
                    }
                    else
                    {
                        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.None);
                    }

                    // Authenticate if credentials provided
                    if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass))
                    {
                        await client.AuthenticateAsync(smtpUser, smtpPass);
                    }

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }
    }
}
