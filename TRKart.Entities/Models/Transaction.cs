using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public int? TransferTransactionID { get; set; }

        // Navigation property for the one-to-one relationship with TransferTransaction
        public Transaction? TransferTransaction { get; set; }

        [Required]
        [Column("Amount", TypeName = "DECIMAL(10, 2)")]
        public decimal Amount { get; set; } 

        [Required]
        [Column("TransactionType", TypeName = "VARCHAR(20)")]
        public string TransactionType { get; set; }

        [Column("Description", TypeName = "TEXT")]
        public string? Description { get; set; }

        [Column("TransactionDate")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime TransactionDate { get; set; }

        [Column("TransactionStatus", TypeName = "VARCHAR(20)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string TransactionStatus { get; set; }

        // Related Transactions (optional one-to-optional one)
        public virtual ICollection<Transaction>? TransferTransactions { get; set; }
    }
}