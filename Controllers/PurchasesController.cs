using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleParts.API.Data;
using VehicleParts.API.DTOs;
using VehicleParts.API.Models;

namespace VehicleParts.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class PurchasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PurchasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchaseInvoice([FromBody] PurchaseInvoiceDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var invoice = new PurchaseInvoice
                {
                    VendorID = dto.VendorId,
                    PurchaseDate = DateTime.UtcNow,
                    TotalCost = dto.Items.Sum(i => i.Quantity * i.UnitCost)
                };

                _context.PurchaseInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                foreach (var itemDto in dto.Items)
                {
                    // Update Stock in Parts table
                    var part = await _context.Parts.FindAsync(itemDto.PartId);
                    if (part != null)
                    {
                        part.StockQuantity += itemDto.Quantity;
                        // Optional: Update cost price to the latest purchase price
                        part.CostPrice = itemDto.UnitCost; 
                    }

                    // Add Invoice Item
                    var purchaseItem = new PurchaseInvoiceItem
                    {
                        PurchaseInvoiceID = invoice.PurchaseInvoiceID,
                        PartID = itemDto.PartId,
                        QuantityPurchased = itemDto.Quantity,
                        UnitCost = itemDto.UnitCost,
                        LineTotal = itemDto.Quantity * itemDto.UnitCost
                    };
                    _context.PurchaseInvoiceItems.Add(purchaseItem);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Purchase invoice created and stock updated.", invoiceId = invoice.PurchaseInvoiceID });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
