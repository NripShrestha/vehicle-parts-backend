using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.DTOs
{
    public class CreateAppointmentDto
    {
        [Required]
        public int VehicleID { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan AppointmentTime { get; set; }

        [Required]
        public string ServiceType { get; set; } = string.Empty;
    }

    public class AppointmentDto
    {
        public int AppointmentID { get; set; }
        public int VehicleID { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public string AppointmentStatus { get; set; } = string.Empty;
    }

    public class CreatePartRequestDto
    {
        [Required]
        public string RequestedPartName { get; set; } = string.Empty;
    }

    public class PartRequestDto
    {
        public int PartRequestID { get; set; }
        public string RequestedPartName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string RequestStatus { get; set; } = string.Empty;
    }

    public class CreateReviewDto
    {
        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;
    }

    public class ReviewDto
    {
        public int ReviewID { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; }
    }

    public class CreateVehicleDto
    {
        [Required]
        public string VehicleNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
    }

    public class CustomerOwnHistoryDto
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public decimal CreditBalance { get; set; }
        public int TotalPurchases { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalAppointments { get; set; }
        public List<CustomerHistoryVehicleDto> Vehicles { get; set; } = new();
        public List<CustomerOwnSalesInvoiceDto> PurchaseHistory { get; set; } = new();
        public List<AppointmentDto> ServiceHistory { get; set; } = new();
        public List<PartRequestDto> PartRequests { get; set; } = new();
        public List<ReviewDto> Reviews { get; set; } = new();
    }

    public class CustomerOwnSalesInvoiceDto
    {
        public int SalesInvoiceID { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public List<CustomerOwnSalesInvoiceItemDto> Items { get; set; } = new();
    }

    public class CustomerOwnSalesInvoiceItemDto
    {
        public int PartID { get; set; }
        public string PartName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
