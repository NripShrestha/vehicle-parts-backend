using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.DTOs
{
    public class CreateStaffDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        [Required]
        public string StaffPosition { get; set; } = string.Empty;

        public string Role { get; set; } = "Staff"; // "Staff" or "Admin"
    }

    public class UpdateStaffDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        [Required]
        public string StaffPosition { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }

    public class StaffResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string StaffPosition { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime DateJoined { get; set; }
    }
}
