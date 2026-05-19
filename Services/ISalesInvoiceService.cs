using VehicleParts.API.DTOs;

namespace VehicleParts.API.Services
{
    public interface ISalesInvoiceService
    {
        Task<SalesInvoiceDto> CreateInvoiceAsync(CreateSalesInvoiceDto createDto);
        Task<SalesInvoiceDto?> GetInvoiceByIdAsync(int id);
        Task<List<SalesInvoiceDto>> GetAllInvoicesAsync();
        Task<SalesInvoiceDto?> UpdatePaymentStatusAsync(int id, UpdateSalesInvoicePaymentDto updateDto);
        Task<InvoiceEmailSendResult> SendInvoiceEmailAsync(int invoiceId);
    }
}
