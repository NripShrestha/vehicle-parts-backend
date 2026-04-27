using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.Models
{
    public class Customer : User
    {
        public string CustomerType { get; set; } = "Regular"; // Regular, Premium
        public decimal CreditBalance { get; set; } = 0;
    }
}
