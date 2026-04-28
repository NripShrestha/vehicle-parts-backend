using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.Models
{
    public class Vehicle
    {
        [Key]
        public int VehicleID { get; set; }
        
        [Required]
        public int CustomerID { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        [MaxLength(20)]
        public string VehicleNumber { get; set; } = string.Empty;
        
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
    }
}
