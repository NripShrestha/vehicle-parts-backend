using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VehicleParts.API.Data;

namespace VehicleParts.API.Services
{
    public class NotificationSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationSchedulerService> _logger;
        // Run once every 24 hours
        private readonly TimeSpan _period = TimeSpan.FromHours(24);

        public NotificationSchedulerService(IServiceProvider serviceProvider, ILogger<NotificationSchedulerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Scheduler Background Service is starting.");

            // Wait a small buffer time after startup before the first run
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Notification Scheduler Background Service is executing check...");
                    await RunNotificationJobAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing notification job.");
                }

                await Task.Delay(_period, stoppingToken);
            }
        }

        private async Task RunNotificationJobAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // 1. Low stock check (< 10)
            var lowStockParts = await context.Parts
                .Where(p => p.StockQuantity < 10)
                .ToListAsync();

            if (lowStockParts.Any())
            {
                var admins = await context.Users
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
                        </tr>";
                }

                adminEmailBody += "</tbody></table>";

                foreach (var adminEmail in admins)
                {
                    if (!string.IsNullOrEmpty(adminEmail))
                    {
                        await emailService.SendEmailAsync(adminEmail, "LOW STOCK WARNING - AutoParts Admin", adminEmailBody);
                    }
                }
            }

            // 2. Overdue payment reminders (> 1 month)
            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
            var overdueInvoices = await context.SalesInvoices
                .Include(si => si.Customer)
                    .ThenInclude(c => c!.User)
                .Where(si => si.CreditAmount > 0 && si.PaymentStatus != "Paid" && si.InvoiceDate < oneMonthAgo)
                .ToListAsync();

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
                    <p>This is a reminder that you have unpaid credit balance invoices overdue for more than one month. Please settle these outstanding balances.</p>
                    <table border='1' cellpadding='5' style='border-collapse: collapse;'>
                        <thead>
                            <tr>
                                <th>Invoice ID</th>
                                <th>Invoice Date</th>
                                <th>Outstanding Credit</th>
                            </tr>
                        </thead>
                        <tbody>";

                foreach (var inv in invoicesList)
                {
                    customerEmailBody += $@"
                        <tr>
                            <td>#INV-{inv.SalesInvoiceID:D5}</td>
                            <td>{inv.InvoiceDate.ToShortDateString()}</td>
                            <td style='font-weight: bold; color: red;'>${inv.CreditAmount:N2}</td>
                        </tr>";
                }

                customerEmailBody += "</tbody></table>";

                await emailService.SendEmailAsync(customerUser.Email, "Overdue Payment Reminder - AutoParts Service", customerEmailBody);
            }
        }
    }
}
