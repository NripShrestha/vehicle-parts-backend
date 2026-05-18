using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleParts.API.Models
{
    public class SalesInvoice
    {
        [Key]
        public int SalesInvoiceID { get; set; }

        [Required]
        public int CustomerID { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        public int StaffID { get; set; }
        public Staff? Staff { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditAmount { get; set; }

        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "Paid"; // Paid, Unpaid, Partial

        public List<SalesInvoiceItem> Items { get; set; } = new();
    }
}
