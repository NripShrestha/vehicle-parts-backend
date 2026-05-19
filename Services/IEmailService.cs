namespace VehicleParts.API.Services
{
    public interface IEmailService
    {
        Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string htmlMessage, int? invoiceId = null);
        EmailSettingsOptions GetEffectiveSettings();
    }
}
