using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("RateLimiting")]
    public class RateLimiting
    {
        [Key]
        [Column("RateLimitID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RateLimitID { get; set; }

        [Required]
        [Column("Identifier", TypeName = "VARCHAR(255)")]
        public string Identifier { get; set; }

        [Required]
        [Column("IdentifierType", TypeName = "VARCHAR(20)")]
        public string IdentifierType { get; set; }

        [Required]
        [Column("Endpoint", TypeName = "VARCHAR(100)")]
        public string Endpoint { get; set; }

        [Column("CustomerID")]
        [ForeignKey("Customer")]
        public int? CustomerID { get; set; }

        // Navigation property
        public Customers? Customer { get; set; }

        [Required]
        [Column("RequestCount")]
        public int RequestCount { get; set; } = 1;

        [Required]
        [Column("FirstRequestAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset FirstRequestAt { get; set; }

        [Required]
        [Column("LastRequestAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset LastRequestAt { get; set; }

        [Required]
        [Column("IsBlocked", TypeName = "BOOLEAN")]
        public bool IsBlocked { get; set; } = false;

        [Column("BlockedUntil")]
        public DateTimeOffset? BlockedUntil { get; set; } = null!;

        [Column("BlockReason", TypeName = "VARCHAR(500)")]
        public string? BlockReason { get; set; } = null!;

        [Column("ViolationCount")]
        public int? ViolationCount { get; set; } = 0;

        [Column("FirstViolationAt")]
        public DateTimeOffset? FirstViolationAt { get; set; } = null!;

        [Column("LastViolationAt")]
        public DateTimeOffset? LastViolationAt { get; set; } = null!;

        [Required]
        [Column("CreatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        [Column("UpdatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset UpdatedAt { get; set; }

        // Computed property to check if block is still active
        [NotMapped]
        public bool IsBlockActive => IsBlocked && BlockedUntil.HasValue && BlockedUntil.Value > DateTimeOffset.UtcNow;
    }
}
