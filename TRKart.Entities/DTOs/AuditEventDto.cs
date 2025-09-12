using System.ComponentModel.DataAnnotations;

namespace TRKart.Entities.DTOs
{
    public class AuditEventDto
    {
        [Required]
        public string EventType { get; set; }

        public string? EventSubType { get; set; }

        public string? EventDetails { get; set; }

        public string? RiskLevel { get; set; } = "LOW";

        public bool ComplianceRequired { get; set; } = false;
    }
}

