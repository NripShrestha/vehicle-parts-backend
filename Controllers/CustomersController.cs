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
                    _context.Vehicles.Any(v => v.CustomerID == c.CustomerID && v.VehicleNumber.ToLower().Contains(query))
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
