using System.Net;
using System.Net.Mail;

namespace VehicleParts.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, int? invoiceId = null)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            bool useMock = emailSettings.GetValue<bool>("UseMock", true);
            string smtpServer = emailSettings.GetValue<string>("SmtpServer") ?? string.Empty;
            int smtpPort = emailSettings.GetValue<int>("SmtpPort", 587);
            string senderName = emailSettings.GetValue<string>("SenderName") ?? "Vehicle Parts API";
            string senderEmail = emailSettings.GetValue<string>("SenderEmail") ?? "noreply@vehicleparts.com";
            string username = emailSettings.GetValue<string>("Username") ?? string.Empty;
            string password = emailSettings.GetValue<string>("Password") ?? string.Empty;
            bool enableSsl = emailSettings.GetValue<bool>("EnableSsl", true);

            // Fallback to mock mode if SMTP settings are not fully specified
            if (string.IsNullOrWhiteSpace(smtpServer) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                useMock = true;
            }

            if (useMock)
            {
                await SaveMockEmailAsync(toEmail, subject, htmlMessage, invoiceId);
                return;
            }

            try
            {
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl
                };

                _logger.LogInformation("Attempting to send email to {ToEmail} via {SmtpServer}...", toEmail, smtpServer);
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {ToEmail}.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via SMTP to {ToEmail}. Falling back to local file save.", toEmail);
                // Fallback to saving locally so the test run is fully visible
                await SaveMockEmailAsync(toEmail, subject, htmlMessage, invoiceId);
            }
        }

        private async Task SaveMockEmailAsync(string toEmail, string subject, string htmlMessage, int? invoiceId = null)
        {
            try
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "SentEmails");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string idString = invoiceId.HasValue ? $"_{invoiceId.Value}" : "";
                string fileName = $"Invoice{idString}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html";
                var filePath = Path.Combine(folderPath, fileName);

                // Add a small developer debug header at the top of the mock file
                var debugMessage = $@"<!-- DEVELOPER MOCK EMAIL INFO
To: {toEmail}
Subject: {subject}
Timestamp: {DateTime.UtcNow}
-->
" + htmlMessage;

                await File.WriteAllTextAsync(filePath, debugMessage);
                
                _logger.LogInformation("[MOCK EMAIL] Local email mock file generated successfully: {FilePath}", filePath);
                Console.WriteLine($"\n[MOCK EMAIL] Saved email for {toEmail} to file: {filePath}\n");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write mock email file.");
            }
        }
    }
}
