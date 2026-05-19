using Microsoft.EntityFrameworkCore;
using VehicleParts.API.Models;

namespace VehicleParts.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } // Updated name
        public DbSet<SalesInvoice> SalesInvoices { get; set; }
        public DbSet<SalesInvoiceItem> SalesInvoiceItems { get; set; }
        
        // New DbSets
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<PartRequest> PartRequests { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Enforce One-to-One Relationships
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.User)
                .WithOne(u => u.Staff)
                .HasForeignKey<Staff>(s => s.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.User)
                .WithOne(u => u.Customer)
                .HasForeignKey<Customer>(c => c.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent Cascade Delete Clashes on SalesInvoice
            modelBuilder.Entity<SalesInvoice>()
                .HasOne(si => si.Staff)
                .WithMany(s => s.SalesInvoices)
                .HasForeignKey(si => si.StaffID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesInvoice>()
                .HasOne(si => si.Customer)
                .WithMany(c => c.SalesInvoices)
                .HasForeignKey(si => si.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Part>()
                .Property(p => p.ImageUrl)
                .HasMaxLength(500);
        }
    }
}
