using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleParts.API.Models
{
    public class Review
    {
        [Key]
        public int ReviewID { get; set; }

        public int CustomerID { get; set; }
        [ForeignKey("CustomerID")]
        public Customer? Customer { get; set; }

        public int? AppointmentID { get; set; }
        [ForeignKey("AppointmentID")]
        public Appointment? Appointment { get; set; }

        public string ServiceType { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;
    }
}
