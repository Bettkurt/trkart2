using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("AuditEvents")]
    public class AuditEvents
    {
        [Key]
        [Column("AuditID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AuditID { get; set; }

        // CustomerID is optional because it can be null for system-generated events like IP based rate limiting.
        [Column("CustomerID", TypeName = "INT")]
        [ForeignKey("Customer")]
        public int? CustomerID { get; set; }

        // Navigation property
        public Customers? Customer { get; set; }

        // SessionID is optional because it can be null for system-generated events like IP based rate limiting.
        [Column("SessionID", TypeName = "INT")]
        [ForeignKey("SessionToken")]
        public int? SessionID { get; set; }

        // Navigation property
        public SessionToken? SessionToken { get; set; }

        [Required]
        [Column("EventType", TypeName = "VARCHAR(50)")]
        public string EventType { get; set; }

        [Column("EventSubType", TypeName = "VARCHAR(50)")]
        public string? EventSubType { get; set; } = null!;

        [Column("EventDetails", TypeName = "TEXT")]
        public string? EventDetails { get; set; } = null!;

        [Column("IPAddress", TypeName = "TEXT")]
        public string? IPAddress { get; set; } = null!;

        [Column("UserAgent", TypeName = "TEXT")]
        public string? UserAgent { get; set; } = null!;

        [Column("RiskLevel", TypeName = "VARCHAR(20)")]
        public string? RiskLevel { get; set; } = "LOW";

        [Column("ComplianceRequired", TypeName = "BOOLEAN")]
        public bool ComplianceRequired { get; set; } = false;

        [Required]
        [Column("CreatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
