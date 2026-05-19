using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.DTOs
{
    public class CreatePartFormDto
    {
        [Required]
        public int VendorID { get; set; }

        [Required, MaxLength(100)]
        public string PartName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal CostPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal SellingPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int ReorderLevel { get; set; }

        /// <summary>Optional image file (field name must be "image"). PNG, JPG, or JPEG, max 10MB.</summary>
        public IFormFile? Image { get; set; }
    }

    public class UpdatePartFormDto
    {
        [Required]
        public int VendorID { get; set; }

        [Required, MaxLength(100)]
        public string PartName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal CostPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal SellingPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int ReorderLevel { get; set; }

        public IFormFile? Image { get; set; }
    }
}
