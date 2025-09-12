using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("SecurityEvents")]
    public class SecurityEvents
    {
        [Key]
        [Column("SecurityEventID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SecurityEventID { get; set; }

        // CustomerID is optional because it can be null for system-generated events.
        [Column("CustomerID", TypeName = "INT")]
        [ForeignKey("Customer")]
        public int? CustomerID { get; set; }

        // Navigation property
        public Customers? Customer { get; set; }

        [Column("Email", TypeName = "VARCHAR(100)")]
        public string? Email { get; set; } = null!;

        [Required]
        [Column("EventType", TypeName = "VARCHAR(50)")]
        public string EventType { get; set; }

        [Required]
        [Column("EventSeverity", TypeName = "VARCHAR(20)")]
        public string EventSeverity { get; set; }

        [Column("EventDetails", TypeName = "TEXT")]
        public string? EventDetails { get; set; } = null!;

        [Column("IPAddress", TypeName = "TEXT")]
        public string? IPAddress { get; set; } = null!;

        [Column("UserAgent", TypeName = "TEXT")]
        public string? UserAgent { get; set; } = null!;

        [Column("DeviceFingerprint", TypeName = "VARCHAR(255)")]
        public string? DeviceFingerprint { get; set; } = null!;

        [Column("GeographicLocation", TypeName = "VARCHAR(100)")]
        public string? GeographicLocation { get; set; } = null!;

        [Column("IsResolved", TypeName = "BOOLEAN")]
        public bool IsResolved { get; set; } = false;

        [Column("ResolvedAt")]
        public DateTimeOffset? ResolvedAt { get; set; } = null!;

        [Column("ResolvedBy", TypeName = "VARCHAR(50)")]
        public string? ResolvedBy { get; set; } = null!;

        [Column("ResolutionNotes", TypeName = "TEXT")]
        public string? ResolutionNotes { get; set; } = null!;

        [Required]
        [Column("CreatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset CreatedAt { get; set; }
    }
}

