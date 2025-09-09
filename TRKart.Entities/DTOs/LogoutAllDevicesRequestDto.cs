using System.ComponentModel.DataAnnotations;

namespace TRKart.Entities.DTOs
{
    public class LogoutAllDevicesRequestDto
    {
        [Required]
        public string Password { get; set; }

        public string? Reason { get; set; }
    }
}

