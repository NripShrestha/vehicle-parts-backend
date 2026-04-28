using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleParts.API.Models
{
    public class SalesInvoiceItem
    {
        [Key]
        public int SalesInvoiceItemID { get; set; }

        [Required]
        public int SalesInvoiceID { get; set; }
        public SalesInvoice? SalesInvoice { get; set; }

        [Required]
        public int PartID { get; set; }
        public Part? Part { get; set; }

        public int QuantitySold { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }
    }
}
