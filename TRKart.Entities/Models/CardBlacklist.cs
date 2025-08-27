using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TRKart.Entities.Enums;

namespace TRKart.Entities.Models
{
    [Table("CardBlacklist")]
    public class CardBlacklist
    {
        [Key]
        [Column("CardBlacklistID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CardBlacklistID { get; set; }

        [Column("CustomerID", TypeName = "INT")]
        [ForeignKey("Customer")]
        public int CustomerID { get; set; }

        // Navigation property
        public Customers Customer { get; set; }

        [Column("OriginalCardID", TypeName = "INT")]
        [ForeignKey("OriginalCard")]
        public int OriginalCardID { get; set; }

        // Navigation property
        public UserCard OriginalCard { get; set; }

        [Required]
        [Column("CardNumber", TypeName = "CHAR(16)")]
        [StringLength(16)]
        public string CardNumber { get; set; }

        [Required]
        [Column("LeftOverBalance", TypeName = "DECIMAL(10, 2)")]
        public decimal LeftOverBalance { get; set; }

        [Required]
        [Column("CardType")]
        public CardType CardType { get; set; }

        [Required]
        [Column("CardExpirationDate")]
        public DateTime CardExpirationDate { get; set; }

        [Required]
        [Column("OriginalCreatedAt")]
        public DateTime OriginalCreatedAt { get; set; }

        [Required]
        [Column("Reason")]
        public CardBlacklistReason Reason { get; set; } // 0: Deactivated, 1: Expired, 2: Reported Lost

        [Required]
        [Column("BlacklistedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime BlacklistedAt { get; set; }
        
        [Column("Notes", TypeName = "TEXT")]
        public string? Notes { get; set; }
    }
}
