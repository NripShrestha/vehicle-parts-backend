using System;
using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.Models
{
    public class Staff : User
    {
        public string StaffPosition { get; set; } = string.Empty;
        public DateTime DateJoined { get; set; } = DateTime.UtcNow;
    }
}
