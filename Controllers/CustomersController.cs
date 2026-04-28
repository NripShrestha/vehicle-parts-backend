using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleParts.API.Data;
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
                    c.User.FullName.ToLower().Contains(query) ||
                    c.User.PhoneNumber.Contains(query) ||
                    c.CustomerID.ToString() == query ||
                    // This searches within the associated vehicles' license plates
                    _context.Vehicles.Any(v => v.CustomerID == c.CustomerID && v.LicensePlate.ToLower().Contains(query))
                )
                .Select(c => new {
                    c.CustomerID,
                    c.User.FullName,
                    c.User.Email,
                    c.User.PhoneNumber,
                    c.CustomerType,
                    c.CreditBalance,
                    // Map your vehicles here
                    Vehicles = _context.Vehicles
                        .Where(v => v.CustomerID == c.CustomerID)
                        .Select(v => new { v.Make, v.Model, v.LicensePlate })
                        .ToList()
                })
                .ToListAsync();

            return Ok(results);
        }

        // --------------------------------------------------------
        // FEATURE F8: Staff can view customer history
        // --------------------------------------------------------
        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetCustomerFullDetails(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .Include(c => c.Vehicles)
                // Assuming you have an Invoices or Sales table for history
                // .Include(c => c.SalesHistory) 
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null) return NotFound();

            return Ok(customer);
        }
    }
}
