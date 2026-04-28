using VehicleParts.API.DTOs;

namespace VehicleParts.API.Services
{
    public interface ISalesInvoiceService
    {
        Task<SalesInvoiceDto> CreateInvoiceAsync(CreateSalesInvoiceDto createDto);
        Task<SalesInvoiceDto?> GetInvoiceByIdAsync(int id);
        Task<List<SalesInvoiceDto>> GetAllInvoicesAsync();
        Task<bool> SendInvoiceEmailAsync(int invoiceId);
    }
}
