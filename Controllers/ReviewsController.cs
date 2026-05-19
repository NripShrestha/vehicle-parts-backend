using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleParts.API.Data;
using VehicleParts.API.DTOs;

namespace VehicleParts.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("services")]
        public async Task<ActionResult<IEnumerable<ServiceReviewAdminDto>>> GetServiceReviews()
        {
            var reviews = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Customer)
                    .ThenInclude(c => c!.User)
                .Include(r => r.Appointment)
                    .ThenInclude(a => a!.Vehicle)
                .Where(r => r.AppointmentID != null)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            var results = reviews.Select(r =>
            {
                var vehicle = r.Appointment?.Vehicle;
                return new ServiceReviewAdminDto
                {
                    ReviewID = r.ReviewID,
                    CustomerID = r.CustomerID,
                    CustomerName = r.Customer?.User?.FullName ?? "Unknown",
                    CustomerEmail = r.Customer?.User?.Email,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ReviewDate = r.ReviewDate,
                    AppointmentID = r.AppointmentID,
                    ServiceType = r.ServiceType,
                    VehicleName = vehicle == null
                        ? string.Empty
                        : $"{vehicle.Brand} {vehicle.Model}".Trim()
                };
            }).ToList();

            return Ok(results);
        }
    }
}
