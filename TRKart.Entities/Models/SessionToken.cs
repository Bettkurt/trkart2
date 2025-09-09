using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("SessionToken")]
    public class SessionToken
    {
        [Key]
        [Column("SessionID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SessionID { get; set; }

        [Column("CustomerID", TypeName = "INT")]
        [ForeignKey("Customer")]
        public int CustomerID { get; set; }

        // Navigation property for the one-to-many relationship with Customer
        public Customers Customer { get; set; }

        [Column("AccessToken", TypeName = "VARCHAR(500)")]
        public string? AccessToken { get; set; } = null!;

        [Required]
        [Column("RefreshToken", TypeName = "VARCHAR(500)")]
        public string RefreshToken { get; set; }

        [Column("AccessTokenExpiration")]
        public DateTimeOffset? AccessTokenExpiration { get; set; } = null!;

        [Required]
        [Column("RefreshTokenExpiration")]
        public DateTimeOffset RefreshTokenExpiration { get; set; }

        [Required]
        [Column("RefreshTokenCreatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset RefreshTokenCreatedAt { get; set; }

        [Column("LastUsedAt")]
        public DateTimeOffset? LastUsedAt { get; set; } = null!;

        [Column("UsageCount")]
        public int UsageCount { get; set; } = 0;

        [Required]
        [Column("IsRevoked", TypeName = "BOOLEAN")]
        public bool IsRevoked { get; set; } = false;

        [Column("RevokedAt")]
        public DateTimeOffset? RevokedAt { get; set; } = null!;

        [Column("RevokeReason", TypeName = "VARCHAR(200)")]
        public string? RevokeReason { get; set; } = null!;

        [Column("DeviceInfo", TypeName = "TEXT")]
        public string? DeviceInfo { get; set; } = null!;

        [Column("DeviceFingerprint", TypeName = "VARCHAR(255)")]
        public string? DeviceFingerprint { get; set; } = null!;

        [Column("IPAddress", TypeName = "TEXT")]
        public string? IPAddress { get; set; } = null!;

        [Column("UserAgent", TypeName = "TEXT")]
        public string? UserAgent { get; set; } = null!;

        [Column("IsSuspicious", TypeName = "BOOLEAN")]
        public bool IsSuspicious { get; set; } = false;

        [Column("SuspiciousReason", TypeName = "VARCHAR(200)")]
        public string? SuspiciousReason { get; set; } = null!;

        [Required]
        [Column("CreatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        [Column("UpdatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset UpdatedAt { get; set; }

        // Related TokenBlacklist (optional one-to-one)
        public virtual ICollection<TokenBlacklist> BlacklistedTokens { get; set; }

        // Related AuditEvents (optional one-to-many)
        public virtual ICollection<AuditEvents> AuditEventLogs { get; set; }
    }
}