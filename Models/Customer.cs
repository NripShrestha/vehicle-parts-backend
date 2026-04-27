using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        public int UserID { get; set; }
        public User? User { get; set; }

        public string CustomerType { get; set; } = "Regular"; // Regular, Premium
        public decimal CreditBalance { get; set; } = 0;
    }
}
