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
    [Authorize(Roles = "Admin")] // Secured: Only Admin can manage vendors
    public class VendorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VendorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Vendors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vendor>>> GetVendors()
        {
            var vendors = await _context.Vendors.ToListAsync();
            return Ok(vendors);
        }

        // GET: api/Vendors/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Vendor>> GetVendor(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);

            if (vendor == null)
            {
                return NotFound(new { message = $"Vendor with ID {id} not found." });
            }

            return Ok(vendor);
        }

        // POST: api/Vendors
        [HttpPost]
        public async Task<ActionResult<Vendor>> CreateVendor([FromBody] CreateVendorDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var vendor = new Vendor
            {
                VendorName = dto.VendorName,
                VendorPhone = dto.VendorPhone,
                VendorEmail = dto.VendorEmail,
                VendorAddress = dto.VendorAddress
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVendor), new { id = vendor.VendorID }, vendor);
        }

        // PUT: api/Vendors/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVendor(int id, [FromBody] UpdateVendorDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var vendor = await _context.Vendors.FindAsync(id);

            if (vendor == null)
            {
                return NotFound(new { message = $"Vendor with ID {id} not found." });
            }

            vendor.VendorName = dto.VendorName;
            vendor.VendorPhone = dto.VendorPhone;
            vendor.VendorEmail = dto.VendorEmail;
            vendor.VendorAddress = dto.VendorAddress;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VendorExists(id))
                {
                    return NotFound(new { message = $"Vendor with ID {id} not found." });
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Vendors/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
            {
                return NotFound(new { message = $"Vendor with ID {id} not found." });
            }

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Vendor deleted successfully." });
        }

        private bool VendorExists(int id)
        {
            return _context.Vendors.Any(e => e.VendorID == id);
        }
    }
}
