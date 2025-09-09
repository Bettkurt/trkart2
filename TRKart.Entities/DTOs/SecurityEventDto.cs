using System.ComponentModel.DataAnnotations;

namespace TRKart.Entities.DTOs
{
    public class SecurityEventDto
    {
        [Required]
        public string EventType { get; set; }

        [Required]
        public string EventSeverity { get; set; }

        public string? EventDetails { get; set; }

        public string? Email { get; set; }

        public string? DeviceFingerprint { get; set; }

        public string? GeographicLocation { get; set; }

        public string? IPAddress { get; set; }

        public string? UserAgent { get; set; }
    }
}

