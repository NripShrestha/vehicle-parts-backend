namespace VehicleParts.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage, int? invoiceId = null);
    }
}
