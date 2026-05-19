using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleParts.API.Data;
using VehicleParts.API.DTOs;

namespace VehicleParts.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Staff")]
    public class PartRequestsController : ControllerBase
    {
        private static readonly string[] AllowedStatuses =
        {
            "Pending",
            "Approved",
            "Rejected",
            "Fulfilled"
        };

        private readonly ApplicationDbContext _context;

        public PartRequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffPartRequestDto>>> GetAll()
        {
            var requests = await _context.PartRequests
                .AsNoTracking()
                .Include(r => r.Customer)
                    .ThenInclude(c => c!.User)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return Ok(requests.Select(MapStaffPartRequest).ToList());
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<StaffPartRequestDto>> UpdateStatus(
            int id,
            [FromBody] UpdatePartRequestStatusDto request)
        {
            if (string.IsNullOrWhiteSpace(request.RequestStatus))
            {
                return BadRequest(new { message = "Request status is required." });
            }

            var normalizedStatus = request.RequestStatus.Trim();
            if (!AllowedStatuses.Contains(normalizedStatus, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Request status must be Pending, Approved, Rejected, or Fulfilled."
                });
            }

            var partRequest = await _context.PartRequests
                .Include(r => r.Customer)
                    .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(r => r.PartRequestID == id);

            if (partRequest == null)
            {
                return NotFound(new { message = $"Part request with ID {id} was not found." });
            }

            partRequest.RequestStatus = char.ToUpperInvariant(normalizedStatus[0]) +
                normalizedStatus[1..].ToLowerInvariant();

            await _context.SaveChangesAsync();

            return Ok(MapStaffPartRequest(partRequest));
        }

        private static StaffPartRequestDto MapStaffPartRequest(Models.PartRequest partRequest)
        {
            return new StaffPartRequestDto
            {
                PartRequestID = partRequest.PartRequestID,
                CustomerID = partRequest.CustomerID,
                CustomerName = partRequest.Customer?.User?.FullName ?? "Unknown",
                CustomerEmail = partRequest.Customer?.User?.Email,
                CustomerPhone = partRequest.Customer?.User?.PhoneNumber,
                RequestedPartName = partRequest.RequestedPartName,
                RequestDate = partRequest.RequestDate,
                RequestStatus = partRequest.RequestStatus
            };
        }
    }
}
