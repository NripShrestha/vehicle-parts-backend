using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.DTOs
{
    public class CreateSalesInvoiceDto
    {
        [Required]
        public int CustomerID { get; set; }

        [Required]
        public int StaffID { get; set; }

        public List<CreateSalesInvoiceItemDto> Items { get; set; } = new();
    }

    public class CreateSalesInvoiceItemDto
    {
        [Required]
        public int PartID { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class SalesInvoiceDto
    {
        public int SalesInvoiceID { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int StaffID { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public List<SalesInvoiceItemDto> Items { get; set; } = new();
    }

    public class SalesInvoiceItemDto
    {
        public int SalesInvoiceItemID { get; set; }
        public int PartID { get; set; }
        public string PartName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
