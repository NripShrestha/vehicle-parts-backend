using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleParts.API.Data;
using VehicleParts.API.DTOs;
using VehicleParts.API.Models;
using VehicleParts.API.Services;

namespace VehicleParts.API.Controllers
{
    [Route("api/customer-self-service")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class CustomerSelfServiceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISalesInvoiceService _salesInvoiceService;

        public CustomerSelfServiceController(
            ApplicationDbContext context,
            ISalesInvoiceService salesInvoiceService)
        {
            _context = context;
            _salesInvoiceService = salesInvoiceService;
        }

        [HttpGet("catalog")]
        public async Task<ActionResult<IEnumerable<MarketplacePartDto>>> GetCatalog()
        {
            var parts = await _context.Parts
                .AsNoTracking()
                .Where(p => p.StockQuantity > 0)
                .OrderBy(p => p.PartName)
                .Select(p => new MarketplacePartDto
                {
                    PartID = p.PartID,
                    PartName = p.PartName,
                    Category = p.Category,
                    SellingPrice = p.SellingPrice,
                    StockQuantity = p.StockQuantity,
                    ReorderLevel = p.ReorderLevel,
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync();

            return Ok(parts);
        }

        [HttpPost("purchases")]
        public async Task<ActionResult<CustomerOwnSalesInvoiceDto>> CreatePurchase(
            [FromBody] CreateCustomerPurchaseDto request)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new { message = "At least one part is required to complete a purchase." });
            }

            try
            {
                var invoice = await _salesInvoiceService.CreateCustomerPurchaseAsync(
                    customer.CustomerID,
                    request);

                return CreatedAtAction(nameof(GetHistory), invoice);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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

        [HttpGet("profile")]
        public async Task<ActionResult<CustomerProfileDto>> GetProfile()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null || customer.User == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            return Ok(MapProfile(customer));
        }

        [HttpPut("profile")]
        public async Task<ActionResult<CustomerProfileDto>> UpdateProfile([FromBody] UpdateCustomerProfileDto request)
        {
            var customer = await GetCurrentTrackedCustomerAsync();
            if (customer == null || customer.User == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            var fullName = request.FullName.Trim();
            var email = request.Email.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                return BadRequest(new { message = "Full name is required." });
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "Email is required." });
            }

            var emailExists = await _context.Users.AnyAsync(u =>
                u.UserID != customer.UserID &&
                u.Email.ToLower() == email.ToLower());

            if (emailExists)
            {
                return BadRequest(new { message = "Email is already registered to another account." });
            }

            customer.User.FullName = fullName;
            customer.User.Email = email;
            customer.User.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
            customer.User.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();

            await _context.SaveChangesAsync();

            return Ok(MapProfile(customer));
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

            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.CustomerID == customer.CustomerID)
                .Select(v => new CustomerHistoryVehicleDto
                {
                    VehicleID = v.VehicleID,
                    VehicleNumber = v.VehicleNumber,
                    Brand = v.Brand,
                    Model = v.Model,
                    Year = v.Year
                })
                .ToListAsync();

            return Ok(new CustomerOwnHistoryDto
            {
                CustomerID = customer.CustomerID,
                FullName = customer.User?.FullName ?? string.Empty,
                CustomerType = customer.CustomerType,
                CreditBalance = customer.CreditBalance,
                TotalPurchases = purchaseHistory.Count,
                TotalSpent = purchaseHistory.Sum(i => i.TotalAmount),
                TotalAppointments = serviceHistory.Count,
                Vehicles = vehicles,
                PurchaseHistory = purchaseHistory,
                ServiceHistory = serviceHistory,
                PartRequests = partRequests,
                Reviews = reviews
            });
        }

        [HttpGet("vehicles")]
        public async Task<ActionResult<IEnumerable<CustomerHistoryVehicleDto>>> GetMyVehicles()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.CustomerID == customer.CustomerID)
                .Select(v => new CustomerHistoryVehicleDto
                {
                    VehicleID = v.VehicleID,
                    VehicleNumber = v.VehicleNumber,
                    Brand = v.Brand,
                    Model = v.Model,
                    Year = v.Year
                })
                .ToListAsync();

            return Ok(vehicles);
        }

        [HttpPost("vehicles")]
        public async Task<ActionResult<CustomerHistoryVehicleDto>> RegisterVehicle([FromBody] CreateVehicleDto request)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            if (string.IsNullOrWhiteSpace(request.VehicleNumber))
            {
                return BadRequest(new { message = "Vehicle number is required." });
            }

            var vehicleNumber = request.VehicleNumber.Trim();
            var vehicleExists = await _context.Vehicles.AnyAsync(v =>
                v.CustomerID == customer.CustomerID &&
                v.VehicleNumber.ToLower() == vehicleNumber.ToLower());

            if (vehicleExists)
            {
                return BadRequest(new { message = "This vehicle number is already registered for the current customer." });
            }

            var vehicle = new Vehicle
            {
                CustomerID = customer.CustomerID,
                VehicleNumber = vehicleNumber,
                Brand = request.Brand?.Trim() ?? string.Empty,
                Model = request.Model?.Trim() ?? string.Empty,
                Year = request.Year
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return Ok(MapVehicle(vehicle));
        }

        [HttpPut("vehicles/{id}")]
        public async Task<ActionResult<CustomerHistoryVehicleDto>> UpdateVehicle(int id, [FromBody] UpdateVehicleDto request)
        {
            var customer = await GetCurrentTrackedCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            if (string.IsNullOrWhiteSpace(request.VehicleNumber))
            {
                return BadRequest(new { message = "Vehicle number is required." });
            }

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleID == id && v.CustomerID == customer.CustomerID);

            if (vehicle == null)
            {
                return NotFound(new { message = $"Vehicle with ID {id} was not found." });
            }

            var vehicleNumber = request.VehicleNumber.Trim();
            var duplicateVehicle = await _context.Vehicles.AnyAsync(v =>
                v.VehicleID != id &&
                v.CustomerID == customer.CustomerID &&
                v.VehicleNumber.ToLower() == vehicleNumber.ToLower());

            if (duplicateVehicle)
            {
                return BadRequest(new { message = "This vehicle number is already registered for the current customer." });
            }

            vehicle.VehicleNumber = vehicleNumber;
            vehicle.Brand = request.Brand?.Trim() ?? string.Empty;
            vehicle.Model = request.Model?.Trim() ?? string.Empty;
            vehicle.Year = request.Year;

            await _context.SaveChangesAsync();

            return Ok(MapVehicle(vehicle));
        }

        [HttpDelete("vehicles/{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var customer = await GetCurrentTrackedCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { message = "Customer account was not found for the current user." });
            }

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleID == id && v.CustomerID == customer.CustomerID);

            if (vehicle == null)
            {
                return NotFound(new { message = $"Vehicle with ID {id} was not found." });
            }

            var hasAppointmentHistory = await _context.Appointments
                .AnyAsync(a => a.VehicleID == id);

            if (hasAppointmentHistory)
            {
                return BadRequest(new
                {
                    message = "Vehicle cannot be deleted because it already has appointment history. Keep it to preserve service records."
                });
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Vehicle deleted successfully." });
        }

        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return null;
            }

            return await _context.Customers
                .AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserID == userId.Value);
        }

        private async Task<Customer?> GetCurrentTrackedCustomerAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return null;
            }

            return await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserID == userId.Value);
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }

        private static CustomerProfileDto MapProfile(Customer customer)
        {
            return new CustomerProfileDto
            {
                FullName = customer.User?.FullName ?? string.Empty,
                Email = customer.User?.Email ?? string.Empty,
                PhoneNumber = customer.User?.PhoneNumber,
                Address = customer.User?.Address,
                CustomerType = customer.CustomerType,
                CreditBalance = customer.CreditBalance
            };
        }

        private static CustomerHistoryVehicleDto MapVehicle(Vehicle vehicle)
        {
            return new CustomerHistoryVehicleDto
            {
                VehicleID = vehicle.VehicleID,
                VehicleNumber = vehicle.VehicleNumber,
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Year = vehicle.Year
            };
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
