using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleParts.API.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public User? User { get; set; }

        public string CustomerType { get; set; } = "Regular";
        public decimal CreditBalance { get; set; } = 0;

        // Navigation Properties based on 3NF
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<PartRequest> PartRequests { get; set; } = new List<PartRequest>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
