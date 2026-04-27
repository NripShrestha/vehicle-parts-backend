using System;
using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.Models
{
    public class Staff
    {
        [Key]
        public int StaffID { get; set; }

        public int UserID { get; set; }
        public User? User { get; set; }

        public string StaffPosition { get; set; } = string.Empty;
        public DateTime DateJoined { get; set; } = DateTime.UtcNow;
    }
}
