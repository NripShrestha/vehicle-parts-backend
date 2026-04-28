using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleParts.API.Models
{
    public class Staff
    {
        [Key]
        public int StaffID { get; set; }

        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public User? User { get; set; }

        public string StaffPosition { get; set; } = string.Empty;
        public DateTime DateJoined { get; set; } = DateTime.UtcNow;

        public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
    }
}
