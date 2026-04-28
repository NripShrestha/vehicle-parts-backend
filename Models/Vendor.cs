using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.Models
{
    public class Vendor
    {
        [Key]
        public int VendorID { get; set; }
        [Required, MaxLength(100)]
        public string VendorName { get; set; } = string.Empty;
        [Required, Phone]
        public string VendorPhone { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string VendorEmail { get; set; } = string.Empty;
        [MaxLength(255)]
        public string? VendorAddress { get; set; }

        public ICollection<Part> Parts { get; set; } = new List<Part>();
        public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
    }
}
