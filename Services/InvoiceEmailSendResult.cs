namespace VehicleParts.API.Services
{
    public class InvoiceEmailSendResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public EmailDeliveryMode? DeliveryMode { get; init; }
        public string? MockFilePath { get; init; }

        public static InvoiceEmailSendResult NotFound() => new()
        {
            Success = false,
            Message = "Invoice not found."
        };

        public static InvoiceEmailSendResult NoCustomerEmail() => new()
        {
            Success = false,
            Message = "Customer has no valid email address on file."
        };

        public static InvoiceEmailSendResult FromEmailResult(EmailSendResult emailResult)
        {
            if (!emailResult.Success)
            {
                return new InvoiceEmailSendResult
                {
                    Success = false,
                    Message = emailResult.ErrorMessage ?? "Failed to send invoice email."
                };
            }

            if (emailResult.Mode == EmailDeliveryMode.Smtp)
            {
                return new InvoiceEmailSendResult
                {
                    Success = true,
                    DeliveryMode = EmailDeliveryMode.Smtp,
                    Message = "Invoice email was delivered to the customer's inbox."
                };
            }

            return new InvoiceEmailSendResult
            {
                Success = true,
                DeliveryMode = EmailDeliveryMode.Mock,
                MockFilePath = emailResult.MockFilePath,
                Message =
                    "Invoice email was NOT sent to the customer's inbox. SMTP is not configured (UseMock or missing credentials). " +
                    $"A preview was saved locally at: {emailResult.MockFilePath}"
            };
        }
    }
}
