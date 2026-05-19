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
    [Authorize(Roles = "Staff,Admin")] // Task specifies Staff functionality
    public class CustomersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var results = await _context.Customers
                .Include(c => c.User)
                .Select(c => new {
                    c.CustomerID,
                    FullName = c.User != null ? c.User.FullName : string.Empty,
                    Email = c.User != null ? c.User.Email : string.Empty,
                    PhoneNumber = c.User != null ? c.User.PhoneNumber : null,
                    c.CustomerType,
                    c.CreditBalance,
                    Vehicles = _context.Vehicles
                        .Where(v => v.CustomerID == c.CustomerID)
                        .Select(v => new { v.Brand, v.Model, v.VehicleNumber })
                        .ToList()
                })
                .ToListAsync();

            return Ok(results);
        }

        // FEATURE F10: Search Customers
        // Search by: Name, Phone, ID, or Vehicle Plate
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return BadRequest("Search term is required.");
            }

            var query = term.ToLower();

            var results = await _context.Customers
                .Include(c => c.User)
                // Assuming you have a Vehicles navigation property in your Customer model
                // .Include(c => c.Vehicles) 
                .Where(c => 
                    (c.User != null && c.User.FullName.ToLower().Contains(query)) ||
                    (c.User != null && (c.User.PhoneNumber ?? string.Empty).Contains(query)) ||
                    c.CustomerID.ToString() == query ||
                    // This searches within the associated vehicles' license plates
                    _context.Vehicles.Any(v => v.CustomerID == c.CustomerID && v.VehicleNumber.ToLower().Contains(query))
                )
                .Select(c => new {
                    c.CustomerID,
                    FullName = c.User != null ? c.User.FullName : string.Empty,
                    Email = c.User != null ? c.User.Email : string.Empty,
                    PhoneNumber = c.User != null ? c.User.PhoneNumber : null,
                    c.CustomerType,
                    c.CreditBalance,
                    // Map your vehicles here
                    Vehicles = _context.Vehicles
                        .Where(v => v.CustomerID == c.CustomerID)
                        .Select(v => new { v.Brand, v.Model, v.VehicleNumber })
                        .ToList()
                })
                .ToListAsync();

            return Ok(results);
        }

        [HttpPost("register-with-vehicle")]
        public async Task<IActionResult> RegisterCustomer([FromBody] CreateCustomerWithVehicleDto request)
        {
            // 1. Create User
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Role = "Customer",
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!") // Default password
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 2. Create Customer Profile
            var customer = new Customer
            {
                UserID = user.UserID,
                CustomerType = "Regular"
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // 3. Create Vehicle Details
            var vehicle = new Vehicle
            {
                CustomerID = customer.CustomerID,
                VehicleNumber = request.VehicleNumber,
                Brand = request.Brand,
                Model = request.Model
            };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Customer and vehicle registered successfully." });
        }

        // --------------------------------------------------------
        // FEATURE F8: Staff can view customer history
        // --------------------------------------------------------
        [HttpGet("{id}/details")]
        [HttpGet("{id}/history")]
        public async Task<ActionResult<CustomerHistoryDto>> GetCustomerFullDetails(int id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found." });
            }

            var salesHistory = await _context.SalesInvoices
                .AsNoTracking()
                .Where(invoice => invoice.CustomerID == id)
                .Include(invoice => invoice.Items)
                    .ThenInclude(item => item.Part)
                .Include(invoice => invoice.Staff)
                    .ThenInclude(staff => staff!.User)
                .OrderByDescending(invoice => invoice.InvoiceDate)
                .ToListAsync();

            var response = new CustomerHistoryDto
            {
                CustomerID = customer.CustomerID,
                FullName = customer.User?.FullName ?? string.Empty,
                Email = customer.User?.Email ?? string.Empty,
                PhoneNumber = customer.User?.PhoneNumber ?? string.Empty,
                CustomerType = customer.CustomerType,
                CreditBalance = customer.CreditBalance,
                TotalInvoices = salesHistory.Count,
                TotalSpent = salesHistory.Sum(invoice => invoice.TotalAmount),
                LastPurchaseDate = salesHistory.Any() ? salesHistory.First().InvoiceDate : null,
                Vehicles = customer.Vehicles
                    .Select(vehicle => new CustomerHistoryVehicleDto
                    {
                        VehicleID = vehicle.VehicleID,
                        VehicleNumber = vehicle.VehicleNumber,
                        Brand = vehicle.Brand,
                        Model = vehicle.Model,
                        Year = vehicle.Year
                    })
                    .ToList(),
                SalesHistory = salesHistory
                    .Select(invoice => new CustomerHistoryInvoiceDto
                    {
                        SalesInvoiceID = invoice.SalesInvoiceID,
                        InvoiceDate = invoice.InvoiceDate,
                        StaffID = invoice.StaffID,
                        StaffName = invoice.Staff?.User?.FullName ?? "Unknown",
                        TotalAmount = invoice.TotalAmount,
                        DiscountAmount = invoice.DiscountAmount,
                        CreditAmount = invoice.CreditAmount,
                        PaymentStatus = invoice.PaymentStatus,
                        Items = invoice.Items
                            .Select(item => new CustomerHistoryInvoiceItemDto
                            {
                                SalesInvoiceItemID = item.SalesInvoiceItemID,
                                PartID = item.PartID,
                                PartName = item.Part?.PartName ?? "Unknown",
                                QuantitySold = item.QuantitySold,
                                UnitPrice = item.UnitPrice,
                                LineTotal = item.LineTotal
                            })
                            .ToList()
                    })
                    .ToList()
            };

            return Ok(response);
        }
    }
}
