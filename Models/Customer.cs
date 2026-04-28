using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        public int UserID { get; set; }
        public User? User { get; set; }

        public string CustomerType { get; set; } = "Regular"; 
        public decimal CreditBalance { get; set; } = 0;

        // Navigation property for Requirement F10
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}
