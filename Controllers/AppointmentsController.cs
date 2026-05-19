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
    public class AppointmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffAppointmentDto>>> GetAll()
        {
            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Vehicle)
                .Include(a => a.Customer)
                    .ThenInclude(c => c!.User)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            var results = appointments.Select(a =>
            {
                var vehicle = a.Vehicle;
                return new StaffAppointmentDto
                {
                    AppointmentID = a.AppointmentID,
                    CustomerID = a.CustomerID,
                    CustomerName = a.Customer?.User?.FullName ?? "Unknown",
                    VehicleID = a.VehicleID,
                    VehicleNumber = vehicle?.VehicleNumber ?? string.Empty,
                    VehicleName = vehicle == null
                        ? string.Empty
                        : $"{vehicle.Brand} {vehicle.Model}".Trim(),
                    AppointmentDate = a.AppointmentDate,
                    AppointmentTime = a.AppointmentTime,
                    ServiceType = a.ServiceType,
                    AppointmentStatus = a.AppointmentStatus
                };
            }).ToList();

            return Ok(results);
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<StaffAppointmentDto>> UpdateStatus(
            int id,
            [FromBody] UpdateAppointmentStatusDto request)
        {
            var allowedStatuses = new[] { "Pending", "Completed", "Cancelled" };
            var normalizedStatus = request.AppointmentStatus?.Trim() ?? string.Empty;

            if (!allowedStatuses.Contains(normalizedStatus, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Appointment status must be Pending, Completed, or Cancelled."
                });
            }

            var appointment = await _context.Appointments
                .Include(a => a.Vehicle)
                .Include(a => a.Customer)
                    .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(a => a.AppointmentID == id);

            if (appointment == null)
            {
                return NotFound(new { message = $"Appointment with ID {id} was not found." });
            }

            appointment.AppointmentStatus = char.ToUpperInvariant(normalizedStatus[0]) +
                normalizedStatus[1..].ToLowerInvariant();

            await _context.SaveChangesAsync();

            var vehicle = appointment.Vehicle;
            return Ok(new StaffAppointmentDto
            {
                AppointmentID = appointment.AppointmentID,
                CustomerID = appointment.CustomerID,
                CustomerName = appointment.Customer?.User?.FullName ?? "Unknown",
                VehicleID = appointment.VehicleID,
                VehicleNumber = vehicle?.VehicleNumber ?? string.Empty,
                VehicleName = vehicle == null
                    ? string.Empty
                    : $"{vehicle.Brand} {vehicle.Model}".Trim(),
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                ServiceType = appointment.ServiceType,
                AppointmentStatus = appointment.AppointmentStatus
            });
        }
    }
}
