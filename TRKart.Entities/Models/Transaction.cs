using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TRKart.Entities.Enums;

namespace TRKart.Entities.Models
{
    [Table("Transaction")]
    public class Transaction
    {
        [Key]
        [Column("TransactionID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionID { get; set; }

        [Column("CardID", TypeName = "INT")]
        [ForeignKey("UserCard")]
        public int? CardID { get; set; }

        // Navigation property for the one-to-many relationship with UserCard
        public UserCard? UserCard { get; set; }

        // Foreign key for Wallet (for wallet transactions)
        [Column("WalletID", TypeName = "INT")]
        [ForeignKey("Wallet")]
        public int? WalletId { get; set; }

        // Navigation property for Wallet
        public Wallet? Wallet { get; set; }

        // In case of a transfer transaction between cards and/or customers, this will point to the
        // counterpart of a transfer transaction. E.g., if a TransferOut transaction is done, this
        // will point to the corresponding TransferIn transaction and vice versa.
        // Otherwise, it will be null.
        [Column("TransferTransactionID", TypeName = "INT")]
        [ForeignKey("TransferTransaction")]
        public int? TransferTransactionID { get; set; }

        // Navigation property for the one-to-one relationship with TransferTransaction
        public Transaction? TransferTransaction { get; set; }

        [Required]
        [Column("Amount", TypeName = "DECIMAL(10, 2)")]
        public decimal Amount { get; set; } 

        [Required]
        [Column("TransactionType")]
        public TransactionType TransactionType { get; set; }

        // Helper property to determine if this is a wallet transaction
        [NotMapped]
        public bool IsWalletTransaction => WalletId.HasValue;

        [Column("Description", TypeName = "TEXT")]
        public string? Description { get; set; }

        [Column("ExternalRef")]
        public string? ExternalRef { get; set; }

        [Column("PaymentMethod")]
        public string? PaymentMethod { get; set; }

        [Column("FeeAmount", TypeName = "decimal(18, 2)")]
        public decimal? FeeAmount { get; set; }

        [Column("Note")]
        public string? Note { get; set; }

        [Column("TransactionDate")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime TransactionDate { get; set; }

        [Column("TransactionStatus")]
        public string TransactionStatus { get; set; }

        [NotMapped]
        public TransactionStatus Status 
        { 
            get => Enum.Parse<TransactionStatus>(TransactionStatus); 
            set => TransactionStatus = value.ToString(); 
        }

        public Transaction()
        {
            // Set default status in constructor
            TransactionStatus = TRKart.Entities.Enums.TransactionStatus.Pending.ToString();
        }

        // Related Transactions (optional one-to-optional one)
        public virtual ICollection<Transaction>? TransferTransactions { get; set; }
    }
}