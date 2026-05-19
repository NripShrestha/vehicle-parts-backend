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
        private const long MaxImageSizeBytes = 10 * 1024 * 1024;
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png",
            ".jpg",
            ".jpeg"
        };
        private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/png",
            "image/jpeg"
        };

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PartsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // --------------------------------------------------------
        // FEATURE: View Parts Catalog
        // --------------------------------------------------------
        
        // GET: api/Parts
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<IEnumerable<Part>>> GetParts()
        {
            var parts = await _context.Parts.ToListAsync();
            return Ok(parts);
        }

        // GET: api/Parts/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Staff")]
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

            part.ImageUrl = null;
            _context.Parts.Add(part);
            await _context.SaveChangesAsync();

            // Returns a 201 Created status code with a link to the new resource
            return CreatedAtAction(nameof(GetPart), new { id = part.PartID }, part);
        }

        // PUT: api/Parts/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Secured: Only Admin
        public async Task<ActionResult<Part>> PutPart(int id, [FromBody] Part part)
        {
            if (id != part.PartID)
            {
                return BadRequest(new { message = "Part ID in the URL does not match the ID in the body." });
            }

            var existingPart = await _context.Parts.FindAsync(id);
            if (existingPart == null)
            {
                return NotFound(new { message = $"Part with ID {id} not found." });
            }

            existingPart.VendorID = part.VendorID;
            existingPart.PartName = part.PartName;
            existingPart.Category = part.Category;
            existingPart.CostPrice = part.CostPrice;
            existingPart.SellingPrice = part.SellingPrice;
            existingPart.StockQuantity = part.StockQuantity;
            existingPart.ReorderLevel = part.ReorderLevel;

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

            return Ok(existingPart);
        }

        // POST: api/Parts/5/image
        [HttpPost("{id}/image")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UploadPartImage(int id, [FromForm] IFormFile? image)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
            {
                return NotFound(new { message = $"Part with ID {id} not found." });
            }

            var validationError = await ValidateImageAsync(image);
            if (validationError != null)
            {
                return BadRequest(new { message = validationError });
            }

            var extension = Path.GetExtension(image!.FileName).ToLowerInvariant();
            var uploadRoot = GetPartUploadRoot();
            Directory.CreateDirectory(uploadRoot);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = GetSafePartImagePath(uploadRoot, fileName);
            if (filePath == null)
            {
                return BadRequest(new { message = "Invalid image file path." });
            }

            var oldImageUrl = part.ImageUrl;

            await using (var stream = System.IO.File.Create(filePath))
            {
                await image.CopyToAsync(stream);
            }

            part.ImageUrl = $"/uploads/parts/{fileName}";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                DeleteFileIfExists(filePath);
                throw;
            }

            DeletePartImageFile(oldImageUrl);

            return Ok(new
            {
                message = "Part image uploaded successfully.",
                imageUrl = part.ImageUrl,
                part
            });
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

            var oldImageUrl = part.ImageUrl;
            _context.Parts.Remove(part);
            await _context.SaveChangesAsync();
            DeletePartImageFile(oldImageUrl);

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
            return _context.Parts.Any(e => e.PartID == id);
        }

        private async Task<string?> ValidateImageAsync(IFormFile? image)
        {
            if (image == null)
            {
                return "Image file is required. Use multipart/form-data with a file field named image.";
            }

            if (image.Length == 0)
            {
                return "Image file cannot be empty.";
            }

            if (image.Length > MaxImageSizeBytes)
            {
                return "Image file size cannot exceed 10MB.";
            }

            var extension = Path.GetExtension(image.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            {
                return "Only PNG, JPG, and JPEG images are allowed.";
            }

            if (string.IsNullOrWhiteSpace(image.ContentType) || !AllowedImageContentTypes.Contains(image.ContentType))
            {
                return "Only PNG, JPG, and JPEG images are allowed.";
            }

            if (!await HasAllowedImageSignatureAsync(image))
            {
                return "Uploaded file content is not a valid PNG or JPG image.";
            }

            return null;
        }

        private static async Task<bool> HasAllowedImageSignatureAsync(IFormFile image)
        {
            var header = new byte[8];
            await using var stream = image.OpenReadStream();
            var bytesRead = await stream.ReadAsync(header);

            var isPng = bytesRead >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A;

            var isJpeg = bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF;

            return isPng || isJpeg;
        }

        private string GetPartUploadRoot()
        {
            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            return Path.Combine(webRoot, "uploads", "parts");
        }

        private static string? GetSafePartImagePath(string uploadRoot, string fileName)
        {
            var rootPath = Path.GetFullPath(uploadRoot);
            var fullPath = Path.GetFullPath(Path.Combine(rootPath, fileName));

            return fullPath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                ? fullPath
                : null;
        }

        private void DeletePartImageFile(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) ||
                !imageUrl.StartsWith("/uploads/parts/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fileName = Path.GetFileName(imageUrl);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var filePath = GetSafePartImagePath(GetPartUploadRoot(), fileName);
            if (filePath == null)
            {
                return;
            }

            DeleteFileIfExists(filePath);
        }

        private static void DeleteFileIfExists(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}
