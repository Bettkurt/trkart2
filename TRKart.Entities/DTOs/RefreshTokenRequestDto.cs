using System.ComponentModel.DataAnnotations;

namespace TRKart.Entities.DTOs
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; }

        public string? DeviceFingerprint { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
