using Microsoft.EntityFrameworkCore;
using VehicleParts.API.Data;
using VehicleParts.API.DTOs;
using VehicleParts.API.Models;

namespace VehicleParts.API.Services
{
    public class SalesInvoiceService : ISalesInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public SalesInvoiceService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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

            invoice.Subtotal = subtotal;
            invoice.TotalAmount = subtotal - invoice.DiscountAmount;

            _context.SalesInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Automatically send the invoice email to the customer
            try
            {
                await SendInvoiceEmailAsync(invoice.SalesInvoiceID);
            }
            catch (Exception ex)
            {
                // Log the exception but do not fail the invoice transaction itself
                Console.WriteLine($"[EMAIL ERROR] Automatic invoice email failed: {ex.Message}");
            }

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
            var invoice = await _context.SalesInvoices
                .Include(i => i.Items)
                    .ThenInclude(item => item.Part)
                .Include(i => i.Customer)
                    .ThenInclude(c => c!.User)
                .Include(i => i.Staff)
                    .ThenInclude(s => s!.User)
                .FirstOrDefaultAsync(i => i.SalesInvoiceID == invoiceId);

            if (invoice == null || invoice.Customer?.User?.Email == null) return false;

            string customerEmail = invoice.Customer.User.Email;
            string customerName = invoice.Customer.User.FullName;
            string staffName = invoice.Staff?.User?.FullName ?? "Staff Member";
            string dateString = invoice.InvoiceDate.ToString("MMMM dd, yyyy HH:mm");

            // Format table rows
            string itemsRowsHtml = "";
            foreach (var item in invoice.Items)
            {
                string partName = item.Part?.PartName ?? "Unknown Part";
                itemsRowsHtml += $@"
                <tr>
                    <td style=""padding: 12px; border-bottom: 1px solid #e2e8f0; text-align: left; color: #334155; font-size: 14px;"">{partName}</td>
                    <td style=""padding: 12px; border-bottom: 1px solid #e2e8f0; text-align: center; color: #334155; font-size: 14px;"">{item.QuantitySold}</td>
                    <td style=""padding: 12px; border-bottom: 1px solid #e2e8f0; text-align: right; color: #334155; font-size: 14px;"">${item.UnitPrice:N2}</td>
                    <td style=""padding: 12px; border-bottom: 1px solid #e2e8f0; text-align: right; color: #0f172a; font-weight: 600; font-size: 14px;"">${item.LineTotal:N2}</td>
                </tr>";
            }

            // Payment status badge
            string paymentStatusColor = invoice.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase) ? "#10b981" : "#f59e0b";
            string paymentStatusBg = invoice.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase) ? "#ecfdf5" : "#fffbeb";
            string paymentStatusHtml = $@"
                <span style=""display: inline-block; padding: 4px 12px; font-size: 12px; font-weight: 700; text-transform: uppercase; border-radius: 9999px; background-color: {paymentStatusBg}; color: {paymentStatusColor}; border: 1px solid {paymentStatusColor};"">
                    {invoice.PaymentStatus}
                </span>";

            // Loyalty Program discount highlight
            string discountSectionHtml = "";
            if (invoice.DiscountAmount > 0)
            {
                discountSectionHtml = $@"
                <div style=""display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; font-size: 14px; color: #16a34a; font-weight: 600; background-color: #f0fdf4; padding: 8px 12px; border-radius: 6px; border: 1px dashed #10b981;"">
                    <span>Loyalty Discount (10%):</span>
                    <span>-${invoice.DiscountAmount:N2}</span>
                </div>";
            }

            string htmlBody = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Your Invoice - AutoPart Inventory</title>
    <link href=""https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800&display=swap"" rel=""stylesheet"">
    <style>
        body {{
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;
            background-color: #f8fafc;
            margin: 0;
            padding: 0;
            -webkit-font-smoothing: antialiased;
        }}
    </style>
</head>
<body style=""background-color: #f8fafc; padding: 20px; font-family: 'Inter', sans-serif;"">
    <div style=""max-width: 650px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.05), 0 4px 6px -4px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0;"">
        
        <!-- Header -->
        <div style=""background-color: #1e293b; padding: 32px; text-align: center; border-bottom: 4px solid #3b82f6;"">
            <h1 style=""margin: 0; font-size: 24px; font-weight: 800; color: #ffffff; letter-spacing: -0.5px; text-transform: uppercase;"">
                <span style=""color: #3b82f6;"">AutoPart</span> Inventory
            </h1>
            <p style=""margin: 6px 0 0 0; font-size: 14px; color: #94a3b8; font-weight: 500;"">High-Quality Parts & Professional Sales</p>
        </div>

        <div style=""padding: 32px;"">
            <!-- Invoice Meta Block -->
            <div style=""display: flex; justify-content: space-between; border-bottom: 2px solid #f1f5f9; padding-bottom: 24px; margin-bottom: 24px;"">
                <div>
                    <span style=""font-size: 12px; font-weight: 700; text-transform: uppercase; color: #64748b; letter-spacing: 0.5px;"">Invoice Number</span>
                    <h2 style=""margin: 4px 0 0 0; font-size: 20px; font-weight: 800; color: #0f172a;"">#INV-{invoice.SalesInvoiceID:D5}</h2>
                </div>
                <div style=""text-align: right;"">
                    <span style=""font-size: 12px; font-weight: 700; text-transform: uppercase; color: #64748b; letter-spacing: 0.5px;"">Payment Status</span>
                    <div style=""margin-top: 4px;"">{paymentStatusHtml}</div>
                </div>
            </div>

            <!-- Billing Grid -->
            <div style=""display: flex; justify-content: space-between; margin-bottom: 32px; gap: 20px;"">
                <div style=""flex: 1;"">
                    <h4 style=""margin: 0 0 8px 0; font-size: 12px; font-weight: 700; text-transform: uppercase; color: #64748b; letter-spacing: 0.5px;"">Billed To:</h4>
                    <p style=""margin: 0; font-size: 15px; font-weight: 700; color: #0f172a;"">{customerName}</p>
                    <p style=""margin: 4px 0 0 0; font-size: 14px; color: #475569;"">{customerEmail}</p>
                    <p style=""margin: 2px 0 0 0; font-size: 14px; color: #64748b;"">{invoice.Customer?.User?.PhoneNumber ?? ""}</p>
                </div>
                <div style=""flex: 1; text-align: right;"">
                    <h4 style=""margin: 0 0 8px 0; font-size: 12px; font-weight: 700; text-transform: uppercase; color: #64748b; letter-spacing: 0.5px;"">Details:</h4>
                    <p style=""margin: 0; font-size: 14px; color: #475569;""><strong style=""color: #0f172a;"">Date:</strong> {dateString}</p>
                    <p style=""margin: 4px 0 0 0; font-size: 14px; color: #475569;""><strong style=""color: #0f172a;"">Sales Representative:</strong> {staffName}</p>
                </div>
            </div>

            <!-- Items Table -->
            <table style=""width: 100%; border-collapse: collapse; margin-bottom: 32px;"">
                <thead>
                    <tr style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; border-bottom: 2px solid #cbd5e1;"">
                        <th style=""padding: 12px; text-align: left; font-size: 12px; font-weight: 700; text-transform: uppercase; color: #475569; letter-spacing: 0.5px;"">Part Description</th>
                        <th style=""padding: 12px; text-align: center; font-size: 12px; font-weight: 700; text-transform: uppercase; color: #475569; letter-spacing: 0.5px; width: 60px;"">Qty</th>
                        <th style=""padding: 12px; text-align: right; font-size: 12px; font-weight: 700; text-transform: uppercase; color: #475569; letter-spacing: 0.5px; width: 100px;"">Unit Price</th>
                        <th style=""padding: 12px; text-align: right; font-size: 12px; font-weight: 700; text-transform: uppercase; color: #475569; letter-spacing: 0.5px; width: 100px;"">Total</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsRowsHtml}
                </tbody>
            </table>

            <!-- Summary Block -->
            <div style=""display: flex; justify-content: flex-end;"">
                <div style=""width: 100%; max-width: 320px;"">
                    <div style=""display: flex; justify-content: space-between; margin-bottom: 8px; font-size: 14px; color: #475569;"">
                        <span>Subtotal:</span>
                        <span>${invoice.Subtotal:N2}</span>
                    </div>
                    
                    {discountSectionHtml}

                    <div style=""display: flex; justify-content: space-between; margin-bottom: 12px; font-size: 14px; color: #475569;"">
                        <span>Credit Applied:</span>
                        <span>-${invoice.CreditAmount:N2}</span>
                    </div>

                    <div style=""border-top: 2px solid #e2e8f0; padding-top: 12px; display: flex; justify-content: space-between; align-items: center;"">
                        <span style=""font-size: 16px; font-weight: 700; color: #0f172a;"">Total Paid:</span>
                        <span style=""font-size: 22px; font-weight: 800; color: #1e293b;"">${invoice.TotalAmount:N2}</span>
                    </div>
                </div>
            </div>
        </div>

        <!-- Footer -->
        <div style=""background-color: #f8fafc; padding: 24px; text-align: center; border-top: 1px solid #e2e8f0; font-size: 13px; color: #64748b;"">
            <p style=""margin: 0; font-weight: 600; color: #475569;"">Thank you for your business!</p>
            <p style=""margin: 4px 0 0 0;"">If you have any questions about this invoice, please contact support.</p>
        </div>
    </div>
</body>
</html>";

            string subject = $"Invoice #INV-{invoice.SalesInvoiceID:D5} from AutoPart Inventory";
            
            await _emailService.SendEmailAsync(customerEmail, subject, htmlBody, invoice.SalesInvoiceID);
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
                Subtotal = invoice.Subtotal,
                DiscountAmount = invoice.DiscountAmount,
                TotalAmount = invoice.TotalAmount,
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
