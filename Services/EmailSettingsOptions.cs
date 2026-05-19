namespace VehicleParts.API.Services
{
    public class EmailSettingsOptions
    {
        public const string SectionName = "EmailSettings";

        public bool UseMock { get; set; } = true;
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SenderName { get; set; } = "Vehicle Parts API";
        public string SenderEmail { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;

        public bool HasSmtpCredentials =>
            !string.IsNullOrWhiteSpace(SmtpServer) &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);

        public bool WillUseMock => UseMock || !HasSmtpCredentials;
    }
}
