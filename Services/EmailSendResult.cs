namespace VehicleParts.API.Services
{
    public enum EmailDeliveryMode
    {
        Smtp,
        Mock,
        Failed
    }

    public class EmailSendResult
    {
        public bool Success { get; init; }
        public EmailDeliveryMode Mode { get; init; }
        public string? MockFilePath { get; init; }
        public string? ErrorMessage { get; init; }

        public static EmailSendResult SmtpSuccess() => new()
        {
            Success = true,
            Mode = EmailDeliveryMode.Smtp
        };

        public static EmailSendResult MockSuccess(string filePath) => new()
        {
            Success = true,
            Mode = EmailDeliveryMode.Mock,
            MockFilePath = filePath
        };

        public static EmailSendResult Failure(string message) => new()
        {
            Success = false,
            Mode = EmailDeliveryMode.Failed,
            ErrorMessage = message
        };
    }
}
