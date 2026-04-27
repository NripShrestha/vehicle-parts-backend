using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.DTOs
{
    public class CreateVendorDto
    {
        [Required]
        [MaxLength(100)]
        public string VendorName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string VendorPhone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string VendorEmail { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? VendorAddress { get; set; }
    }

    public class UpdateVendorDto : CreateVendorDto
    {
    }
}
