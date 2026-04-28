using Microsoft.EntityFrameworkCore;
using VehicleParts.API.Data;
using VehicleParts.API.DTOs;
using VehicleParts.API.Models;

namespace VehicleParts.API.Services
{
    public class SalesInvoiceService : ISalesInvoiceService
    {
        private readonly ApplicationDbContext _context;

        public SalesInvoiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SalesInvoiceDto> CreateInvoiceAsync(CreateSalesInvoiceDto createDto)
        {
            var invoice = new SalesInvoice
            {
                CustomerID = createDto.CustomerID,
                StaffID = createDto.StaffID,
                InvoiceDate = DateTime.UtcNow,
                PaymentStatus = "Paid" // Default for now
            };

            decimal subtotal = 0;

            foreach (var itemDto in createDto.Items)
            {
                var part = await _context.Parts.FindAsync(itemDto.PartID);
                if (part == null) throw new Exception($"Part with ID {itemDto.PartID} not found.");
                if (part.StockQuantity < itemDto.Quantity) throw new Exception($"Insufficient stock for part {part.PartName}. Available: {part.StockQuantity}");

                var lineTotal = part.SellingPrice * itemDto.Quantity;
                subtotal += lineTotal;

                invoice.Items.Add(new SalesInvoiceItem
                {
                    PartID = itemDto.PartID,
                    QuantitySold = itemDto.Quantity,
                    UnitPrice = part.SellingPrice,
                    LineTotal = lineTotal
                });

                // Update stock level
                part.StockQuantity -= itemDto.Quantity;
            }

            // Apply Loyalty Program Discount: 10% if > 5000
            if (subtotal > 5000)
            {
                invoice.DiscountAmount = subtotal * 0.10m;
            }

            invoice.TotalAmount = subtotal - invoice.DiscountAmount;

            _context.SalesInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            return await GetInvoiceByIdAsync(invoice.SalesInvoiceID) ?? throw new Exception("Failed to retrieve created invoice.");
        }

        public async Task<SalesInvoiceDto?> GetInvoiceByIdAsync(int id)
        {
            var invoice = await _context.SalesInvoices
                .Include(i => i.Items)
                    .ThenInclude(item => item.Part)
                .Include(i => i.Customer)
                    .ThenInclude(c => c!.User)
                .Include(i => i.Staff)
                    .ThenInclude(s => s!.User)
                .FirstOrDefaultAsync(i => i.SalesInvoiceID == id);

            if (invoice == null) return null;

            return MapToDto(invoice);
        }

        public async Task<List<SalesInvoiceDto>> GetAllInvoicesAsync()
        {
            var invoices = await _context.SalesInvoices
                .Include(i => i.Items)
                    .ThenInclude(item => item.Part)
                .Include(i => i.Customer)
                    .ThenInclude(c => c!.User)
                .Include(i => i.Staff)
                    .ThenInclude(s => s!.User)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            return invoices.Select(MapToDto).ToList();
        }

        public async Task<bool> SendInvoiceEmailAsync(int invoiceId)
        {
            // Placeholder for email logic
            // In a real app, this would use SmtpClient or a service like SendGrid
            var invoice = await _context.SalesInvoices
                .Include(i => i.Customer)
                    .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(i => i.SalesInvoiceID == invoiceId);

            if (invoice == null || invoice.Customer?.User?.Email == null) return false;

            // Logic to "send" email (e.g., log it or use a mock)
            Console.WriteLine($"Sending invoice {invoiceId} to {invoice.Customer.User.Email}");
            return true;
        }

        private SalesInvoiceDto MapToDto(SalesInvoice invoice)
        {
            return new SalesInvoiceDto
            {
                SalesInvoiceID = invoice.SalesInvoiceID,
                CustomerID = invoice.CustomerID,
                CustomerName = invoice.Customer?.User?.FullName ?? "Unknown",
                StaffID = invoice.StaffID,
                StaffName = invoice.Staff?.User?.FullName ?? "Unknown",
                InvoiceDate = invoice.InvoiceDate,
                TotalAmount = invoice.TotalAmount,
                DiscountAmount = invoice.DiscountAmount,
                CreditAmount = invoice.CreditAmount,
                PaymentStatus = invoice.PaymentStatus,
                Items = invoice.Items.Select(item => new SalesInvoiceItemDto
                {
                    SalesInvoiceItemID = item.SalesInvoiceItemID,
                    PartID = item.PartID,
                    PartName = item.Part?.PartName ?? "Unknown",
                    QuantitySold = item.QuantitySold,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.LineTotal
                }).ToList()
            };
        }
    }
}
