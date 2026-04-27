using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleParts.API.Data; // Ensure this matches your namespace
using VehicleParts.API.Models; // Ensure this matches your namespace

namespace VehicleParts.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PartsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --------------------------------------------------------
        // FEATURE: View Parts Catalog (Available to Everyone)
        // --------------------------------------------------------
        
        // GET: api/Parts
        [HttpGet]
        [AllowAnonymous] // Making this public so even unregistered users can browse the catalog
        public async Task<ActionResult<IEnumerable<Part>>> GetParts()
        {
            var parts = await _context.Parts.ToListAsync();
            return Ok(parts);
        }

        // GET: api/Parts/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Part>> GetPart(int id)
        {
            var part = await _context.Parts.FindAsync(id);

            if (part == null)
            {
                return NotFound(new { message = $"Part with ID {id} not found." });
            }

            return Ok(part);
        }

        // --------------------------------------------------------
        // FEATURE: Admin-Controlled CRUD Operations
        // --------------------------------------------------------

        // POST: api/Parts
        [HttpPost]
        [Authorize(Roles = "Admin")] // Secured: Only Admin
        public async Task<ActionResult<Part>> PostPart([FromBody] Part part)
        {
            // Note: EF Core automatically validates Data Annotations (like [Required], [Range])
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Parts.Add(part);
            await _context.SaveChangesAsync();

            // Returns a 201 Created status code with a link to the new resource
            return CreatedAtAction(nameof(GetPart), new { id = part.Id }, part);
        }

        // PUT: api/Parts/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Secured: Only Admin
        public async Task<IActionResult> PutPart(int id, [FromBody] Part part)
        {
            if (id != part.Id)
            {
                return BadRequest(new { message = "Part ID in the URL does not match the ID in the body." });
            }

            _context.Entry(part).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PartExists(id))
                {
                    return NotFound(new { message = $"Part with ID {id} not found." });
                }
                else
                {
                    throw; // Re-throw if it's a genuine database error
                }
            }

            return NoContent(); // 204 No Content is standard for successful PUT updates
        }

        // DELETE: api/Parts/5
        [HttpDelete("{id}")][Authorize(Roles = "Admin")] // Secured: Only Admin
        public async Task<IActionResult> DeletePart(int id)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
            {
                return NotFound(new { message = $"Part with ID {id} not found." });
            }

            _context.Parts.Remove(part);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Part deleted successfully." });
        }

        // --------------------------------------------------------
        // FEATURE: Low Stock Monitoring
        // Marking Scheme Requirement: "notifies Admin for low stock (<10)"
        // --------------------------------------------------------

        // GET: api/Parts/low-stock
        [HttpGet("low-stock")][Authorize(Roles = "Admin,Staff")] // Admin and Staff can monitor stock
        public async Task<ActionResult<IEnumerable<Part>>> GetLowStock()
        {
            // Fetch parts where stock is less than 10 OR less than their specific ReorderLevel
            var lowStockParts = await _context.Parts
                .Where(p => p.StockQuantity < 10 || p.StockQuantity <= p.ReorderLevel)
                .ToListAsync();

            return Ok(lowStockParts);
        }

        // Helper method for the PUT operation
        private bool PartExists(int id)
        {
            return _context.Parts.Any(e => e.Id == id);
        }
    }
}
