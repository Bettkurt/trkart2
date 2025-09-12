using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("TokenBlacklist")]
    public class TokenBlacklist
    {
        [Key]
        [Column("BlacklistID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BlacklistID { get; set; }

        [Column("SessionID", TypeName = "INT")]
        [ForeignKey("SessionToken")]
        public int SessionID { get; set; }

        // Navigation property for the one-to-many relationship with SessionToken
        public SessionToken SessionToken { get; set; }

        [Required]
        [Column("RefreshToken", TypeName = "VARCHAR(500)")]
        public string RefreshToken { get; set; }

        [Required]
        [Column("BlacklistedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset BlacklistedAt { get; set; }

        [Column("BlacklistedBy", TypeName = "VARCHAR(50)")]
        public string BlacklistedBy { get; set; } = "System";

        [Column("Reason", TypeName = "TEXT")]
        public string Reason { get; set; }

        [Column("IPAddress", TypeName = "TEXT")]
        public string? IPAddress { get; set; } = null!;

        [Column("UserAgent", TypeName = "TEXT")]
        public string? UserAgent { get; set; } = null!;

        [Column("SuspiciousActivity", TypeName = "BOOLEAN")]
        public bool SuspiciousActivity { get; set; } = false;

        [Column("ComplianceRequired", TypeName = "BOOLEAN")]
        public bool ComplianceRequired { get; set; } = false;
    }
}
