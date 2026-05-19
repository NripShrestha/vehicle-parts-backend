using System;

namespace VehicleParts.API.DTOs
{
    public class FinancialReportDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalDiscounts { get; set; }
        public int TotalSalesCount { get; set; }
    }

    public class FinancialSummaryDto
    {
        public FinancialReportDto Daily { get; set; } = new();
        public FinancialReportDto Monthly { get; set; } = new();
        public FinancialReportDto Yearly { get; set; } = new();
    }

    public class TopSpenderDto
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
    }

    public class PendingCreditDto
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal CreditBalance { get; set; }
    }
}
