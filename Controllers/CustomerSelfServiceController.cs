using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleParts.API.Data;
using VehicleParts.API.DTOs;
using VehicleParts.API.Models;

namespace VehicleParts.API.Controllers
{
    [Route("api/customer-self-service")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class CustomerSelfServiceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CustomerSelfServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("appointments")]
        public async Task<ActionResult<AppointmentDto>> BookAppointment([FromBody] CreateAppointmentDto request)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            var vehicle = await _context.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VehicleID == request.VehicleID && v.CustomerID == customer.CustomerID);

            if (vehicle == null)
            {
                return BadRequest(new { message = "Selected vehicle does not belong to the current customer." });
            }

            if (string.IsNullOrWhiteSpace(request.ServiceType))
            {
                return BadRequest(new { message = "Service type is required." });
            }

            if (request.AppointmentTime < TimeSpan.Zero || request.AppointmentTime >= TimeSpan.FromDays(1))
            {
                return BadRequest(new { message = "Appointment time must be a valid time of day." });
            }

            var appointmentDate = DateTime.SpecifyKind(request.AppointmentDate.Date, DateTimeKind.Utc);
            var appointmentDateTime = appointmentDate.Add(request.AppointmentTime);

            if (appointmentDateTime <= DateTime.UtcNow)
            {
                return BadRequest(new { message = "Appointment date and time must be in the future." });
            }

            var appointment = new Appointment
            {
                CustomerID = customer.CustomerID,
                VehicleID = vehicle.VehicleID,
                AppointmentDate = appointmentDate,
                AppointmentTime = request.AppointmentTime,
                ServiceType = request.ServiceType.Trim(),
                AppointmentStatus = "Pending"
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAppointment), new { id = appointment.AppointmentID }, MapAppointment(appointment, vehicle));
        }

        [HttpGet("appointments")]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointments()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Vehicle)
                .Where(a => a.CustomerID == customer.CustomerID)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            return Ok(appointments.Select(a => MapAppointment(a, a.Vehicle)).ToList());
        }

        [HttpGet("appointments/{id}")]
        public async Task<ActionResult<AppointmentDto>> GetAppointment(int id)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            var appointment = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Vehicle)
                .FirstOrDefaultAsync(a => a.AppointmentID == id && a.CustomerID == customer.CustomerID);

            if (appointment == null)
            {
                return NotFound(new { message = $"Appointment with ID {id} was not found." });
            }

            return Ok(MapAppointment(appointment, appointment.Vehicle));
        }

        [HttpPut("appointments/{id}/cancel")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentID == id && a.CustomerID == customer.CustomerID);

            if (appointment == null)
            {
                return NotFound(new { message = $"Appointment with ID {id} was not found." });
            }

            if (appointment.AppointmentStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Appointment is already cancelled." });
            }

            appointment.AppointmentStatus = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment cancelled successfully." });
        }

        [HttpPost("part-requests")]
        public async Task<ActionResult<PartRequestDto>> RequestUnavailablePart([FromBody] CreatePartRequestDto request)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            if (string.IsNullOrWhiteSpace(request.RequestedPartName))
            {
                return BadRequest(new { message = "Requested part name is required." });
            }

            var partRequest = new PartRequest
            {
                CustomerID = customer.CustomerID,
                RequestedPartName = request.RequestedPartName.Trim(),
                RequestDate = DateTime.UtcNow,
                RequestStatus = "Pending"
            };

            _context.PartRequests.Add(partRequest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPartRequests), new { id = partRequest.PartRequestID }, MapPartRequest(partRequest));
        }

        [HttpGet("part-requests")]
        public async Task<ActionResult<IEnumerable<PartRequestDto>>> GetPartRequests()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            var requests = await _context.PartRequests
                .AsNoTracking()
                .Where(r => r.CustomerID == customer.CustomerID)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return Ok(requests.Select(MapPartRequest).ToList());
        }

        [HttpPost("reviews")]
        public async Task<ActionResult<ReviewDto>> SubmitReview([FromBody] CreateReviewDto request)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest(new { message = "Rating must be between 1 and 5." });
            }

            var review = new Review
            {
                CustomerID = customer.CustomerID,
                Rating = request.Rating,
                Comment = request.Comment.Trim(),
                ReviewDate = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReviews), new { id = review.ReviewID }, MapReview(review));
        }

        [HttpGet("reviews")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            var reviews = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.CustomerID == customer.CustomerID)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            return Ok(reviews.Select(MapReview).ToList());
        }

        [HttpGet("history")]
        public async Task<ActionResult<CustomerOwnHistoryDto>> GetHistory()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            var purchases = await _context.SalesInvoices
                .AsNoTracking()
                .Where(i => i.CustomerID == customer.CustomerID)
                .Include(i => i.Items)
                    .ThenInclude(item => item.Part)
                .Include(i => i.Staff)
                    .ThenInclude(staff => staff!.User)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            var purchaseHistory = purchases
                .Select(i => new CustomerOwnSalesInvoiceDto
                {
                    SalesInvoiceID = i.SalesInvoiceID,
                    InvoiceDate = i.InvoiceDate,
                    StaffName = i.Staff != null && i.Staff.User != null ? i.Staff.User.FullName : "Unknown",
                    Subtotal = i.Subtotal,
                    DiscountAmount = i.DiscountAmount,
                    TotalAmount = i.TotalAmount,
                    CreditAmount = i.CreditAmount,
                    PaymentStatus = i.PaymentStatus,
                    Items = i.Items
                        .Select(item => new CustomerOwnSalesInvoiceItemDto
                        {
                            PartID = item.PartID,
                            PartName = item.Part != null ? item.Part.PartName : "Unknown",
                            QuantitySold = item.QuantitySold,
                            UnitPrice = item.UnitPrice,
                            LineTotal = item.LineTotal
                        })
                        .ToList()
                })
                .ToList();

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Vehicle)
                .Where(a => a.CustomerID == customer.CustomerID)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            var serviceHistory = appointments.Select(a => MapAppointment(a, a.Vehicle)).ToList();

            var requests = await _context.PartRequests
                .AsNoTracking()
                .Where(r => r.CustomerID == customer.CustomerID)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            var partRequests = requests.Select(MapPartRequest).ToList();

            var customerReviews = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.CustomerID == customer.CustomerID)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            var reviews = customerReviews.Select(MapReview).ToList();

            return Ok(new CustomerOwnHistoryDto
            {
                CustomerID = customer.CustomerID,
                FullName = customer.User?.FullName ?? string.Empty,
                TotalPurchases = purchaseHistory.Count,
                TotalSpent = purchaseHistory.Sum(i => i.TotalAmount),
                TotalAppointments = serviceHistory.Count,
                PurchaseHistory = purchaseHistory,
                ServiceHistory = serviceHistory,
                PartRequests = partRequests,
                Reviews = reviews
            });
        }

        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            return await _context.Customers
                .AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserID == userId);
        }

        private static AppointmentDto MapAppointment(Appointment appointment, Vehicle? vehicle)
        {
            return new AppointmentDto
            {
                AppointmentID = appointment.AppointmentID,
                VehicleID = appointment.VehicleID,
                VehicleNumber = vehicle?.VehicleNumber ?? string.Empty,
                VehicleName = vehicle == null ? string.Empty : $"{vehicle.Brand} {vehicle.Model}".Trim(),
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                ServiceType = appointment.ServiceType,
                AppointmentStatus = appointment.AppointmentStatus
            };
        }

        private static PartRequestDto MapPartRequest(PartRequest partRequest)
        {
            return new PartRequestDto
            {
                PartRequestID = partRequest.PartRequestID,
                RequestedPartName = partRequest.RequestedPartName,
                RequestDate = partRequest.RequestDate,
                RequestStatus = partRequest.RequestStatus
            };
        }

        private static ReviewDto MapReview(Review review)
        {
            return new ReviewDto
            {
                ReviewID = review.ReviewID,
                Rating = review.Rating,
                Comment = review.Comment,
                ReviewDate = review.ReviewDate
            };
        }
    }
}
