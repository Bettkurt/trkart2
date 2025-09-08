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
        public int CardID { get; set; }

        // Navigation property for the one-to-many relationship with UserCard
        public UserCard UserCard { get; set; }

        // In case of a transfer transaction between cards and/or customers, this will point to the
        // counterpart of a transfer transaction. E.g., if a TransferOut transaction is done, this
        // will point to the corresponding TransferIn transaction and vice versa.
        // Otherwise, it will be null.
        [Column("TransferTransactionID", TypeName = "INT")]
        [ForeignKey("TransferTransaction")]
        public int? TransferTransactionID { get; set; } = null!;

        // Navigation property for the one-to-one relationship with TransferTransaction
        public Transaction? TransferTransaction { get; set; }

        [Required]
        [Column("Amount", TypeName = "DECIMAL(18, 2)")]
        public decimal Amount { get; set; }

        [Column("FeeAmount", TypeName = "decimal(18, 2)")]
        public decimal? FeeAmount { get; set; } = null!;

        // 0: Load, 1: TopUp, 2: Refund, 3: TransferIn, 4: TransferOut, 5: Pay, 
        // 6: SystemTransferIn, 7: SystemTransferOut
        [Required]
        [Column("TransactionType")]
        [EnumDataType(typeof(TransactionType), ErrorMessage = "Invalid transaction type")]
        public TransactionType TransactionType { get; set; }

        [Column("PaymentMethod", TypeName = "VARCHAR(50)")]
        public string? PaymentMethod { get; set; } = null!;

        [Column("ExternalRef", TypeName = "VARCHAR(100)")]
        public string? ExternalRef { get; set; } = null!;

        [Column("Description", TypeName = "TEXT")]
        public string? Description { get; set; } = null!;

        [Column("Note", TypeName = "TEXT")]
        public string? Note { get; set; } = null!;

        [Column("TransactionDate")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset TransactionDate { get; set; }

        [Column("TransactionStatus", TypeName = "VARCHAR(20)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string TransactionStatus { get; set; }

        // Related Transactions (optional one-to-optional one)
        public virtual ICollection<Transaction>? TransferTransactions { get; set; }
    }
}