using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleParts.API.Models
{
    public class PartRequest
    {
        [Key]
        public int PartRequestID { get; set; }

        public int CustomerID { get; set; }
        [ForeignKey("CustomerID")]
        public Customer? Customer { get; set; }

        [Required]
        public string RequestedPartName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public string RequestStatus { get; set; } = "Pending";
    }
}
