using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleParts.API.Models
{
    public class PurchaseInvoiceItem
    {
        [Key]
        public int PurchaseInvoiceItemID { get; set; }
        
        [Required]
        public int PurchaseInvoiceID { get; set; }
        [ForeignKey("PurchaseInvoiceID")]
        public PurchaseInvoice? PurchaseInvoice { get; set; }

        [Required]
        public int PartID { get; set; }
        [ForeignKey("PartID")]
        public Part? Part { get; set; }

        public int QuantityPurchased { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal { get; set; }
    }
}
