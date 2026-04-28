using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleParts.API.Data; // Update to match your DbContext namespace
using VehicleParts.API.DTOs;
using VehicleParts.API.Models;
using VehicleParts.API.Services;

namespace VehicleParts.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            // 1. Check if email exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Email is already registered.");
            }

            // 2. Create User first
            var newUser = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                Role = "Customer" // Default role
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // 3. Create associated Customer record
            var newCustomer = new Customer
            {
                UserID = newUser.UserID,
                CustomerType = "Regular",
                CreditBalance = 0
            };

            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            // 1. Find user and INCLUDE the role-specific tables
            var user = await _context.Users
                .Include(u => u.Customer)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            // 2. Verify password with BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            // 3. Generate token
            var token = _jwtService.GenerateToken(user);

            // 4. Extract Role-Specific IDs for the frontend
            int? customerId = user.Customer?.CustomerID;
            int? staffId = user.Staff?.StaffID;

            return Ok(new 
            { 
                token, 
                user = new 
                { 
                    UserID = user.UserID, 
                    FullName = user.FullName, 
                    Email = user.Email, 
                    Role = user.Role,
                    CustomerID = customerId, // Frontend uses this to view own history
                    StaffID = staffId        // Frontend uses this to create sales invoices
                } 
            });
        }
    }
}
