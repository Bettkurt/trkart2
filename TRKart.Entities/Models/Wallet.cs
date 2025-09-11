using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TRKart.Entities.Enums;

namespace TRKart.Entities.Models
{
    [Table("Wallets")]
    public class Wallet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("WalletID")]
        public int WalletId { get; set; }

        [Required]
        [Column("WalletNumber", TypeName = "CHAR(10)")]
        [StringLength(10)]
        public string WalletNumber { get; set; }

        [Required]
        [Column("CustomerID")]
        public int CustomerID { get; set; }

        [Required]
        [Column("Balance", TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0.00m;

        [Required]
        [Column("Status", TypeName = "integer")]
        public CardStatus Status { get; set; }

        [Required]
        [Column("CreatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("UpdatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CustomerID")]
        public virtual Customers Customer { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
