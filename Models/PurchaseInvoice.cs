using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleParts.API.Models
{
    public class PurchaseInvoice
    {
        [Key]
        public int PurchaseInvoiceID { get; set; }

        [Required]
        public int VendorID { get; set; }
        [ForeignKey("VendorID")]
        public Vendor? Vendor { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public decimal TotalCost { get; set; }

        public ICollection<PurchaseInvoiceItem> Items { get; set; } = new List<PurchaseInvoiceItem>();
    }
}
