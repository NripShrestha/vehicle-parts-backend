using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace VehicleParts.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettingsOptions _settings;
        private readonly ILogger<EmailService> _logger;
        private readonly string _sentEmailsFolder;

        public EmailService(
            IOptions<EmailSettingsOptions> options,
            ILogger<EmailService> logger,
            IWebHostEnvironment environment)
        {
            _settings = options.Value;
            _logger = logger;
            _sentEmailsFolder = Path.Combine(environment.ContentRootPath, "SentEmails");
        }

        public EmailSettingsOptions GetEffectiveSettings() => _settings;

        public async Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string htmlMessage, int? invoiceId = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return EmailSendResult.Failure("Recipient email address is empty.");
            }

            if (_settings.WillUseMock)
            {
                if (!_settings.UseMock && !_settings.HasSmtpCredentials)
                {
                    _logger.LogWarning(
                        "Email to {ToEmail} saved locally: SMTP Username/Password are not configured. Set EmailSettings in appsettings or user-secrets.",
                        toEmail);
                }
                else
                {
                    _logger.LogInformation(
                        "Email to {ToEmail} saved locally (UseMock=true). No message was sent to the recipient inbox.",
                        toEmail);
                }

                return await SaveMockEmailAsync(toEmail, subject, htmlMessage, invoiceId);
            }

            try
            {
                // Gmail and most providers require the From address to match the authenticated account.
                var fromEmail = string.IsNullOrWhiteSpace(_settings.SenderEmail)
                    ? _settings.Username.Trim()
                    : _settings.SenderEmail.Trim();

                if (!fromEmail.Equals(_settings.Username.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "SenderEmail ({SenderEmail}) differs from SMTP Username ({Username}). Using Username as From address to avoid provider rejection.",
                        fromEmail,
                        _settings.Username);
                    fromEmail = _settings.Username.Trim();
                }

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, _settings.SenderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail.Trim());

                using var smtpClient = new SmtpClient(_settings.SmtpServer.Trim(), _settings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_settings.Username.Trim(), _settings.Password),
                    EnableSsl = _settings.EnableSsl
                };

                _logger.LogInformation(
                    "Sending email to {ToEmail} via {SmtpServer}:{Port}...",
                    toEmail,
                    _settings.SmtpServer,
                    _settings.SmtpPort);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email delivered via SMTP to {ToEmail}.", toEmail);
                return EmailSendResult.SmtpSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP send failed for {ToEmail}. Saving a local copy instead.", toEmail);
                var mockResult = await SaveMockEmailAsync(toEmail, subject, htmlMessage, invoiceId);
                if (mockResult.Success)
                {
                    return EmailSendResult.Failure(
                        $"SMTP failed ({ex.Message}). A copy was saved locally at: {mockResult.MockFilePath}");
                }

                return EmailSendResult.Failure($"SMTP failed: {ex.Message}");
            }
        }

        private async Task<EmailSendResult> SaveMockEmailAsync(string toEmail, string subject, string htmlMessage, int? invoiceId = null)
        {
            try
            {
                if (!Directory.Exists(_sentEmailsFolder))
                {
                    Directory.CreateDirectory(_sentEmailsFolder);
                }

                string idString = invoiceId.HasValue ? $"_{invoiceId.Value}" : "";
                string fileName = $"Invoice{idString}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html";
                var filePath = Path.Combine(_sentEmailsFolder, fileName);

                var debugMessage = $@"<!-- DEVELOPER MOCK EMAIL (not delivered to inbox)
To: {toEmail}
Subject: {subject}
Timestamp: {DateTime.UtcNow:O}
-->
" + htmlMessage;

                await File.WriteAllTextAsync(filePath, debugMessage);

                _logger.LogInformation("[MOCK EMAIL] Saved to {FilePath}", filePath);
                return EmailSendResult.MockSuccess(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write mock email file.");
                return EmailSendResult.Failure($"Could not save mock email: {ex.Message}");
            }
        }
    }
}
