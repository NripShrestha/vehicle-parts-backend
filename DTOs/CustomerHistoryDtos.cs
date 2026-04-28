namespace VehicleParts.API.DTOs
{
    public class CustomerHistoryDto
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public decimal CreditBalance { get; set; }
        public int TotalInvoices { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public List<CustomerHistoryVehicleDto> Vehicles { get; set; } = new();
        public List<CustomerHistoryInvoiceDto> SalesHistory { get; set; } = new();
    }

    public class CustomerHistoryVehicleDto
    {
        public int VehicleID { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
    }

    public class CustomerHistoryInvoiceDto
    {
        public int SalesInvoiceID { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int StaffID { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public List<CustomerHistoryInvoiceItemDto> Items { get; set; } = new();
    }

    public class CustomerHistoryInvoiceItemDto
    {
        public int SalesInvoiceItemID { get; set; }
        public int PartID { get; set; }
        public string PartName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
