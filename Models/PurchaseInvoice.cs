using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.Models
{
    public class PurchaseInvoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VendorID { get; set; }
        public Vendor? Vendor { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public decimal TotalCost { get; set; }

        public List<PurchaseItem> Items { get; set; } = new();
    }

    public class PurchaseItem
    {
        [Key]
        public int Id { get; set; }
        public int PurchaseInvoiceId { get; set; }
        
        [Required]
        public int PartID { get; set; }
        public Part? Part { get; set; }

        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}
