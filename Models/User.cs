using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty; // For BCrypt
        public string Role { get; set; } = "Customer"; // Admin, Staff, Customer
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        public Staff? Staff { get; set; }
        public Customer? Customer { get; set; }
    }
}
