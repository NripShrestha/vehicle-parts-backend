using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleParts.API.DTOs;
using VehicleParts.API.Services;

namespace VehicleParts.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires authentication for all sales operations
    public class SalesInvoiceController : ControllerBase
    {
        private readonly ISalesInvoiceService _salesInvoiceService;

        public SalesInvoiceController(ISalesInvoiceService salesInvoiceService)
        {
            _salesInvoiceService = salesInvoiceService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")] // Only Admin or Staff can create invoices
        public async Task<ActionResult<SalesInvoiceDto>> CreateInvoice(CreateSalesInvoiceDto createDto)
        {
            try
            {
                var invoice = await _salesInvoiceService.CreateInvoiceAsync(createDto);
                return CreatedAtAction(nameof(GetInvoice), new { id = invoice.SalesInvoiceID }, invoice);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SalesInvoiceDto>> GetInvoice(int id)
        {
            var invoice = await _salesInvoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();
            return Ok(invoice);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<IEnumerable<SalesInvoiceDto>>> GetAllInvoices()
        {
            var invoices = await _salesInvoiceService.GetAllInvoicesAsync();
            return Ok(invoices);
        }

        [HttpPut("{id}/payment")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<SalesInvoiceDto>> UpdatePaymentStatus(int id, UpdateSalesInvoicePaymentDto updateDto)
        {
            try
            {
                var invoice = await _salesInvoiceService.UpdatePaymentStatusAsync(id, updateDto);
                if (invoice == null)
                {
                    return NotFound(new { message = $"Sales invoice with ID {id} was not found." });
                }

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/send-email")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SendInvoiceEmail(int id)
        {
            var result = await _salesInvoiceService.SendInvoiceEmailAsync(id);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            if (result.DeliveryMode == EmailDeliveryMode.Mock)
            {
                return Ok(new
                {
                    message = result.Message,
                    deliveredToInbox = false,
                    deliveryMode = "mock",
                    mockFilePath = result.MockFilePath
                });
            }

            return Ok(new
            {
                message = result.Message,
                deliveredToInbox = true,
                deliveryMode = "smtp"
            });
        }
    }
}
