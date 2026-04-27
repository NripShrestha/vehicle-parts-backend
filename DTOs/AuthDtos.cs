using System.ComponentModel.DataAnnotations;

namespace VehicleParts.API.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
    }

    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
