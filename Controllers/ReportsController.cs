using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VehicleParts.API.Data;
using VehicleParts.API.DTOs;

namespace VehicleParts.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // FEATURE F1: Admin Financial Reports
        // =========================================================================

        /// <summary>
        /// Calculates total revenue, total discounts given, and total number of sales for Daily (Today).
        /// </summary>
        [HttpGet("financial/daily")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FinancialReportDto>> GetDailyReport()
        {
            var now = DateTime.UtcNow;
            // Define boundary for the start of today (midnight UTC) and tomorrow
            var startOfToday = now.Date;
            var startOfTomorrow = startOfToday.AddDays(1);

            // Filter SalesInvoices occurring today and aggregate totals using GroupBy.
            // If no records are found, DefaultIfEmpty or falling back to 0 prevents errors.
            var stats = await _context.SalesInvoices
                .Where(si => si.InvoiceDate >= startOfToday && si.InvoiceDate < startOfTomorrow)
                .GroupBy(si => 1)
                .Select(g => new FinancialReportDto
                {
                    TotalRevenue = g.Sum(si => si.TotalAmount),
                    TotalDiscounts = g.Sum(si => si.DiscountAmount),
                    TotalSalesCount = g.Count()
                })
                .FirstOrDefaultAsync();

            return Ok(stats ?? new FinancialReportDto());
        }

        /// <summary>
        /// Calculates total revenue, total discounts given, and total number of sales for Monthly (Current Month).
        /// </summary>
        [HttpGet("financial/monthly")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FinancialReportDto>> GetMonthlyReport()
        {
            var now = DateTime.UtcNow;
            // Define boundary for the start of the current month and start of the next month in UTC
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfNextMonth = startOfMonth.AddMonths(1);

            // Filter SalesInvoices falling within the current month and aggregate the values using LINQ
            var stats = await _context.SalesInvoices
                .Where(si => si.InvoiceDate >= startOfMonth && si.InvoiceDate < startOfNextMonth)
                .GroupBy(si => 1)
                .Select(g => new FinancialReportDto
                {
                    TotalRevenue = g.Sum(si => si.TotalAmount),
                    TotalDiscounts = g.Sum(si => si.DiscountAmount),
                    TotalSalesCount = g.Count()
                })
                .FirstOrDefaultAsync();

            return Ok(stats ?? new FinancialReportDto());
        }

        /// <summary>
        /// Calculates total revenue, total discounts given, and total number of sales for Yearly (Current Year).
        /// </summary>
        [HttpGet("financial/yearly")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FinancialReportDto>> GetYearlyReport()
        {
            var now = DateTime.UtcNow;
            // Define boundary for the start of the current year and start of the next year in UTC
            var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfNextYear = startOfYear.AddYears(1);

            // Filter SalesInvoices falling within the current year and aggregate the values using LINQ
            var stats = await _context.SalesInvoices
                .Where(si => si.InvoiceDate >= startOfYear && si.InvoiceDate < startOfNextYear)
                .GroupBy(si => 1)
                .Select(g => new FinancialReportDto
                {
                    TotalRevenue = g.Sum(si => si.TotalAmount),
                    TotalDiscounts = g.Sum(si => si.DiscountAmount),
                    TotalSalesCount = g.Count()
                })
                .FirstOrDefaultAsync();

            return Ok(stats ?? new FinancialReportDto());
        }

        /// <summary>
        /// Calculates and returns a summary containing Daily, Monthly, and Yearly financial reports in one API call.
        /// </summary>
        [HttpGet("financial/summary")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FinancialSummaryDto>> GetFinancialSummary()
        {
            var now = DateTime.UtcNow;

            // Boundaries for Daily (Today UTC)
            var startOfToday = now.Date;
            var startOfTomorrow = startOfToday.AddDays(1);

            // Boundaries for Monthly (Current Month UTC)
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfNextMonth = startOfMonth.AddMonths(1);

            // Boundaries for Yearly (Current Year UTC)
            var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfNextYear = startOfYear.AddYears(1);

            // Run three separate LINQ queries to aggregate results for today, this month, and this year.
            var daily = await _context.SalesInvoices
                .Where(si => si.InvoiceDate >= startOfToday && si.InvoiceDate < startOfTomorrow)
                .GroupBy(si => 1)
                .Select(g => new FinancialReportDto
                {
                    TotalRevenue = g.Sum(si => si.TotalAmount),
                    TotalDiscounts = g.Sum(si => si.DiscountAmount),
                    TotalSalesCount = g.Count()
                })
                .FirstOrDefaultAsync() ?? new FinancialReportDto();

            var monthly = await _context.SalesInvoices
                .Where(si => si.InvoiceDate >= startOfMonth && si.InvoiceDate < startOfNextMonth)
                .GroupBy(si => 1)
                .Select(g => new FinancialReportDto
                {
                    TotalRevenue = g.Sum(si => si.TotalAmount),
                    TotalDiscounts = g.Sum(si => si.DiscountAmount),
                    TotalSalesCount = g.Count()
                })
                .FirstOrDefaultAsync() ?? new FinancialReportDto();

            var yearly = await _context.SalesInvoices
                .Where(si => si.InvoiceDate >= startOfYear && si.InvoiceDate < startOfNextYear)
                .GroupBy(si => 1)
                .Select(g => new FinancialReportDto
                {
                    TotalRevenue = g.Sum(si => si.TotalAmount),
                    TotalDiscounts = g.Sum(si => si.DiscountAmount),
                    TotalSalesCount = g.Count()
                })
                .FirstOrDefaultAsync() ?? new FinancialReportDto();

            return Ok(new FinancialSummaryDto
            {
                Daily = daily,
                Monthly = monthly,
                Yearly = yearly
            });
        }

        // =========================================================================
        // FEATURE F9: Customer Analytics
        // =========================================================================

        /// <summary>
        /// Retrieves customers who have made at least two purchases, treating them as regular customers.
        /// </summary>
        [HttpGet("regular-customers")]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<ActionResult<IEnumerable<RegularCustomerDto>>> GetRegularCustomers()
        {
            var regularCustomers = await _context.Customers
                .Select(c => new RegularCustomerDto
                {
                    CustomerID = c.CustomerID,
                    FullName = c.User != null ? c.User.FullName : string.Empty,
                    Email = c.User != null ? c.User.Email : string.Empty,
                    PhoneNumber = c.User != null ? (c.User.PhoneNumber ?? string.Empty) : string.Empty,
                    PurchaseCount = c.SalesInvoices.Count(),
                    TotalSpent = c.SalesInvoices.Sum(si => si.TotalAmount),
                    LastPurchaseDate = c.SalesInvoices
                        .OrderByDescending(si => si.InvoiceDate)
                        .Select(si => (DateTime?)si.InvoiceDate)
                        .FirstOrDefault()
                })
                .Where(c => c.PurchaseCount >= 2)
                .OrderByDescending(c => c.PurchaseCount)
                .ThenByDescending(c => c.TotalSpent)
                .ToListAsync();

            return Ok(regularCustomers);
        }

        /// <summary>
        /// Retrieves a list of customers ranked by their total successful spending (PaymentStatus = Paid).
        /// </summary>
        [HttpGet("top-spenders")]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<ActionResult<IEnumerable<TopSpenderDto>>> GetTopSpenders()
        {
            // LINQ query joining Customers with Users (to retrieve user profile details) and SalesInvoices.
            // Under the hood, EF Core translates this navigation-based query into SQL LEFT JOINs.
            // We project the customer details and compute the sum of TotalAmount from their invoices.
            var topSpenders = await _context.Customers
                .Select(c => new TopSpenderDto
                {
                    CustomerID = c.CustomerID,
                    FullName = c.User != null ? c.User.FullName : string.Empty,
                    Email = c.User != null ? c.User.Email : string.Empty,
                    TotalSpent = c.SalesInvoices.Sum(si => si.TotalAmount)
                })
                .Where(c => c.TotalSpent > 0)
                .OrderByDescending(c => c.TotalSpent)
                .ToListAsync();

            return Ok(topSpenders);
        }

        /// <summary>
        /// Retrieves a list of customers with unpaid credit balances.
        /// </summary>
        [HttpGet("pending-credits")]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<ActionResult<IEnumerable<PendingCreditDto>>> GetPendingCredits()
        {
            // LINQ query filtering Customers with CreditBalance greater than 0.
            // Accesses the related User profile using navigation properties to load FullName, Email, and Phone.
            var pendingCredits = await _context.Customers
                .Where(c => c.CreditBalance > 0)
                .Select(c => new PendingCreditDto
                {
                    CustomerID = c.CustomerID,
                    FullName = c.User != null ? c.User.FullName : string.Empty,
                    Email = c.User != null ? c.User.Email : string.Empty,
                    PhoneNumber = c.User != null ? (c.User.PhoneNumber ?? string.Empty) : string.Empty,
                    CreditBalance = c.CreditBalance
                })
                .OrderByDescending(c => c.CreditBalance)
                .ToListAsync();

            return Ok(pendingCredits);
        }
        /// <summary>
        /// Manually triggers low stock alerts and sends overdue payment emails.
        /// </summary>
        [HttpPost("trigger-notifications")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> TriggerNotifications([FromServices] VehicleParts.API.Services.IEmailService emailService)
        {
            // 1. Check for low stock parts (< 10)
            var lowStockParts = await _context.Parts
                .Where(p => p.StockQuantity < 10)
                .ToListAsync();

            int lowStockEmailsSent = 0;
            if (lowStockParts.Any())
            {
                var admins = await _context.Users
                    .Where(u => u.Role == "Admin")
                    .Select(u => u.Email)
                    .ToListAsync();

                var adminEmailBody = $@"
                    <h2>Low Stock Alert</h2>
                    <p>The following parts have stock levels below 10 units:</p>
                    <table border='1' cellpadding='5' style='border-collapse: collapse;'>
                        <thead>
                            <tr>
                                <th>Part Name</th>
                                <th>SKU</th>
                                <th>Current Stock</th>
                                <th>Price</th>
                            </tr>
                        </thead>
                        <tbody>";

                foreach (var part in lowStockParts)
                {
                    adminEmailBody += $@"
                        <tr>
                            <td>{part.PartName}</td>
                            <td>{part.PartID}</td>
                            <td style='color: red; font-weight: bold;'>{part.StockQuantity}</td>
                            <td>${part.SellingPrice}</td>
                        </tr>";
                }

                adminEmailBody += "</tbody></table>";

                foreach (var adminEmail in admins)
                {
                    if (!string.IsNullOrEmpty(adminEmail))
                    {
                        await emailService.SendEmailAsync(adminEmail, "LOW STOCK WARNING - AutoParts Admin", adminEmailBody);
                        lowStockEmailsSent++;
                    }
                }
            }

            // 2. Check for customers with unpaid credits overdue for more than 1 month
            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
            var overdueInvoices = await _context.SalesInvoices
                .Include(si => si.Customer)
                    .ThenInclude(c => c!.User)
                .Where(si => si.CreditAmount > 0 && si.PaymentStatus != "Paid" && si.InvoiceDate < oneMonthAgo)
                .ToListAsync();

            int overdueEmailsSent = 0;
            var groupedInvoices = overdueInvoices
                .GroupBy(si => si.Customer)
                .Where(g => g.Key != null && g.Key.User != null && !string.IsNullOrEmpty(g.Key.User.Email));

            foreach (var group in groupedInvoices)
            {
                var customer = group.Key!;
                var customerUser = customer.User!;
                var invoicesList = group.ToList();

                var customerEmailBody = $@"
                    <h2>Overdue Payment Reminder</h2>
                    <p>Dear {customerUser.FullName},</p>
                    <p>This is a reminder that you have unpaid credit balance invoices that are overdue for more than one month. Please settle these outstanding balances at your earliest convenience.</p>
                    <table border='1' cellpadding='5' style='border-collapse: collapse;'>
                        <thead>
                            <tr>
                                <th>Invoice ID</th>
                                <th>Invoice Date</th>
                                <th>Outstanding Credit</th>
                                <th>Overdue Duration</th>
                            </tr>
                        </thead>
                        <tbody>";

                foreach (var inv in invoicesList)
                {
                    var duration = DateTime.UtcNow - inv.InvoiceDate;
                    customerEmailBody += $@"
                        <tr>
                            <td>#INV-{inv.SalesInvoiceID:D5}</td>
                            <td>{inv.InvoiceDate.ToShortDateString()}</td>
                            <td style='font-weight: bold; color: red;'>${inv.CreditAmount:N2}</td>
                            <td>{Math.Floor(duration.TotalDays)} days overdue</td>
                        </tr>";
                }

                customerEmailBody += "</tbody></table><p>Thank you for your business!</p>";

                await emailService.SendEmailAsync(customerUser.Email, "Overdue Payment Reminder - AutoParts Service", customerEmailBody);
                overdueEmailsSent++;
            }

            return Ok(new { 
                message = "Notification job triggered successfully.", 
                lowStockCount = lowStockParts.Count,
                lowStockEmailsSent,
                overdueInvoicesCount = overdueInvoices.Count,
                overdueEmailsSent
            });
        }
    }
}
