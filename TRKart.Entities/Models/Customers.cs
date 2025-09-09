using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("Customers")]
    public class Customers
    {
        // Primary Key
        [Key]
        [Column("CustomerID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerID { get; set; }

        [Required]
        [Column("CustomerNumber", TypeName = "CHAR(10)")]
        public string CustomerNumber { get; set; }

        [Column("FullName", TypeName = "VARCHAR(100)")]
        public string? FullName  { get; set; } = null!;

        [Required]
        [Column("Email", TypeName = "VARCHAR(100)")]
        public string Email { get; set; }

        [Required]
        [Column("VerifiedUser", TypeName = "BOOLEAN")]
        public bool VerifiedUser { get; set; } = true;

        [Column("EmailLastUpdatedAt")]
        public DateTimeOffset? EmailLastUpdatedAt { get; set; } = null!;

        [Required]
        [Column("PasswordHash", TypeName = "VARCHAR(200)")]
        public string PasswordHash { get; set; }

        [Column("PasswordChangedAt")]
        public DateTimeOffset? PasswordChangedAt { get; set; } = null!;

        [Column("LastLoginAt")]
        public DateTimeOffset? LastLoginAt { get; set; } = null!;

        [Column("FailedLoginAttempts")]
        public int FailedLoginAttempts { get; set; } = 0;

        [Column("AccountLockedUntil")]
        public DateTimeOffset? AccountLockedUntil { get; set; } = null!;

        [Required]
        [Column("CreatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("UpdatedAt")]
        public DateTimeOffset? UpdatedAt { get; set; } = null!;

        // Related PasswordHistory (mandatory one-to-optional many)
        public virtual ICollection<PasswordHistory>? PasswordHistory { get; set; } = new List<PasswordHistory>();

        // Related SessionTokens (mandatory one-to-mandatory many)
        public virtual ICollection<SessionToken> SessionTokens { get; set; } = new List<SessionToken>();

        // Related UserCards (mandatory one-to-optional many)
        public virtual ICollection<UserCard>? UserCards { get; set; } = new List<UserCard>();
        
        // Related BlacklistedCards (mandatory one-to-optional many)
        public virtual ICollection<CardBlacklist>? BlacklistedCards { get; set; } = new List<CardBlacklist>();

        // Related AuditEvents (mandatory one-to-optional many)
        public virtual ICollection<AuditEvents>? AuditEventLogs { get; set; } = new List<AuditEvents>();

        // Related SecurityEvents (mandatory one-to-optional many)
        public virtual ICollection<SecurityEvents>? SecurityEventLogs { get; set; } = new List<SecurityEvents>();

        // Related RateLimiting (mandatory one-to-optional many)
        public virtual ICollection<RateLimiting>? RateLimitingLogs { get; set; } = new List<RateLimiting>();
    }
}