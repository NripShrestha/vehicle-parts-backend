using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleParts.API.Models
{
    public class Part
    {
        [Key]
        public int PartID { get; set; }
        
        [Required]
        public int VendorID { get; set; }
        [ForeignKey("VendorID")]
        public Vendor? Vendor { get; set; }

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
    }
}