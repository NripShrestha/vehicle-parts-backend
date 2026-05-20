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
            "image/jpeg",
            "image/jpg",
            "application/octet-stream"
        };

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PartsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<IEnumerable<Part>>> GetParts()
        {
            var parts = await _context.Parts.ToListAsync();
            return Ok(parts);
        }

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

        /// <summary>Create a part using JSON (no image). Upload image separately via POST /api/Parts/{id}/image.</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Part>> PostPart([FromBody] Part part)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            part.ImageUrl = null;
            _context.Parts.Add(part);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPart), new { id = part.PartID }, part);
        }

        /// <summary>Create a part with an optional image in one request (multipart/form-data).</summary>
        [HttpPost("with-image")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Part>> CreatePartWithImage([FromForm] CreatePartFormDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _context.Vendors.AnyAsync(v => v.VendorID == dto.VendorID))
            {
                return BadRequest(new { message = $"Vendor with ID {dto.VendorID} not found." });
            }

            var part = new Part
            {
                VendorID = dto.VendorID,
                PartName = dto.PartName.Trim(),
                Category = dto.Category.Trim(),
                CostPrice = dto.CostPrice,
                SellingPrice = dto.SellingPrice,
                StockQuantity = dto.StockQuantity,
                ReorderLevel = dto.ReorderLevel
            };

            _context.Parts.Add(part);
            await _context.SaveChangesAsync();

            if (dto.Image != null)
            {
                var uploadResult = await SavePartImageAsync(part, dto.Image);
                if (uploadResult.Error != null)
                {
                    return BadRequest(new { message = uploadResult.Error, part });
                }

                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetPart), new { id = part.PartID }, part);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
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

                throw;
            }

            return Ok(existingPart);
        }

        /// <summary>Update part fields and optionally replace the image (multipart/form-data).</summary>
        [HttpPut("{id}/with-image")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Part>> UpdatePartWithImage(int id, [FromForm] UpdatePartFormDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingPart = await _context.Parts.FindAsync(id);
            if (existingPart == null)
            {
                return NotFound(new { message = $"Part with ID {id} not found." });
            }

            if (!await _context.Vendors.AnyAsync(v => v.VendorID == dto.VendorID))
            {
                return BadRequest(new { message = $"Vendor with ID {dto.VendorID} not found." });
            }

            existingPart.VendorID = dto.VendorID;
            existingPart.PartName = dto.PartName.Trim();
            existingPart.Category = dto.Category.Trim();
            existingPart.CostPrice = dto.CostPrice;
            existingPart.SellingPrice = dto.SellingPrice;
            existingPart.StockQuantity = dto.StockQuantity;
            existingPart.ReorderLevel = dto.ReorderLevel;

            if (dto.Image != null)
            {
                var uploadResult = await SavePartImageAsync(existingPart, dto.Image);
                if (uploadResult.Error != null)
                {
                    return BadRequest(new { message = uploadResult.Error });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(existingPart);
        }

        [HttpPost("{id}/image")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadPartImage(int id, IFormFile? image)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
            {
                return NotFound(new { message = $"Part with ID {id} not found." });
            }

            var uploadResult = await SavePartImageAsync(part, image);
            if (uploadResult.Error != null)
            {
                return BadRequest(new { message = uploadResult.Error });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Part image uploaded successfully.",
                imageUrl = part.ImageUrl,
                part
            });
        }

        [HttpDelete("{id}/image")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePartImage(int id)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
            {
                return NotFound(new { message = $"Part with ID {id} not found." });
            }

            DeletePartImageFile(part.ImageUrl);
            part.ImageUrl = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Part image removed.", part });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
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

        [HttpGet("low-stock")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<IEnumerable<Part>>> GetLowStock()
        {
            var lowStockParts = await _context.Parts
                .Where(p => p.StockQuantity < 10 || p.StockQuantity <= p.ReorderLevel)
                .ToListAsync();

            return Ok(lowStockParts);
        }

        private bool PartExists(int id) => _context.Parts.Any(e => e.PartID == id);

        private async Task<(string? Error, string? FilePath)> SavePartImageAsync(Part part, IFormFile? image)
        {
            var validationError = await ValidateImageAsync(image);
            if (validationError != null)
            {
                return (validationError, null);
            }

            var extension = Path.GetExtension(image!.FileName).ToLowerInvariant();
            var uploadRoot = GetPartUploadRoot();
            Directory.CreateDirectory(uploadRoot);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = GetSafePartImagePath(uploadRoot, fileName);
            if (filePath == null)
            {
                return ("Invalid image file path.", null);
            }

            var oldImageUrl = part.ImageUrl;

            await using (var stream = System.IO.File.Create(filePath))
            {
                await image.CopyToAsync(stream);
            }

            part.ImageUrl = $"/uploads/parts/{fileName}";

            try
            {
                DeletePartImageFile(oldImageUrl);
            }
            catch
            {
                DeleteFileIfExists(filePath);
                part.ImageUrl = oldImageUrl;
                throw;
            }

            return (null, filePath);
        }

        private async Task<string?> ValidateImageAsync(IFormFile? image)
        {
            if (image == null)
            {
                return "Image file is required. Use multipart/form-data with a file field named 'image'.";
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

            if (!string.IsNullOrWhiteSpace(image.ContentType) &&
                !AllowedImageContentTypes.Contains(image.ContentType))
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
