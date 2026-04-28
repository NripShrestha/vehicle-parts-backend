using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleParts.API.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentID { get; set; }

        public int CustomerID { get; set; }
        [ForeignKey("CustomerID")]
        public Customer? Customer { get; set; }

        public int VehicleID { get; set; }
        [ForeignKey("VehicleID")]
        public Vehicle? Vehicle { get; set; }

        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public string AppointmentStatus { get; set; } = "Pending";
    }
}
