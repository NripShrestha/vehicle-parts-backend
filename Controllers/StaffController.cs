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
    public class StaffController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStaff()
        {
            var staffs = await _context.Staffs
                .Include(s => s.User)
                .Select(s => new StaffResponseDto
                {
                    Id = s.StaffID,
                    FullName = s.User!.FullName,
                    Email = s.User.Email,
                    PhoneNumber = s.User.PhoneNumber,
                    Address = s.User.Address,
                    StaffPosition = s.StaffPosition,
                    Role = s.User.Role,
                    DateJoined = s.DateJoined
                })
                .ToListAsync();

            return Ok(staffs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStaffById(int id)
        {
            var staff = await _context.Staffs.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffID == id);
            if (staff == null || staff.User == null)
            {
                return NotFound("Staff not found.");
            }

            var response = new StaffResponseDto
            {
                Id = staff.StaffID,
                FullName = staff.User.FullName,
                Email = staff.User.Email,
                PhoneNumber = staff.User.PhoneNumber,
                Address = staff.User.Address,
                StaffPosition = staff.StaffPosition,
                Role = staff.User.Role,
                DateJoined = staff.DateJoined
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Email is already registered.");
            }

            var newUser = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                Role = request.Role
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var newStaff = new Staff
            {
                UserID = newUser.UserID,
                StaffPosition = request.StaffPosition,
                DateJoined = DateTime.UtcNow
            };

            _context.Staffs.Add(newStaff);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Staff registered successfully." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStaff(int id, [FromBody] UpdateStaffDto request)
        {
            var staff = await _context.Staffs.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffID == id);
            if (staff == null || staff.User == null)
            {
                return NotFound("Staff not found.");
            }

            staff.User.FullName = request.FullName;
            staff.User.PhoneNumber = request.PhoneNumber;
            staff.User.Address = request.Address;
            staff.User.Role = request.Role;

            staff.StaffPosition = request.StaffPosition;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Staff updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var staff = await _context.Staffs.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffID == id);
            if (staff == null)
            {
                return NotFound("Staff not found.");
            }

            if (staff.User != null)
            {
                _context.Users.Remove(staff.User);
            }
            else
            {
                _context.Staffs.Remove(staff);
            }
            
            await _context.SaveChangesAsync();

            return Ok(new { message = "Staff deleted successfully." });
        }
    }
}
