using System;
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

        [Column("CardID")]
        [ForeignKey("UserCard")]
        public int CardID { get; set; }
        // Navigation property for the one-to-many relationship with UserCard
        public UserCard UserCard { get; set; }

        // In case of a transfer transaction between cards and/or customers, this will point to the
        // counterpart of a transfer transaction. E.g., if a TransferOut transaction is done, this
        // will point to the TransferIn transaction and vice versa.
        [Column("TransferTransactionID")]
        [ForeignKey("TransferTransaction")]
        public int? TransferTransactionID { get; set; }
        // Navigation property for the one-to-one relationship with TransferTransaction
        public Transaction? TransferTransaction { get; set; }

        [Column("Amount")]
        [Required]
        public decimal Amount { get; set; } 

        [Column("TransactionType")]
        [Required]
        public string TransactionType { get; set; }

        [Column("Description")]
        public string? Description { get; set; }

        [Column("TransactionDate")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime TransactionDate { get; set; }

        [Column("TransactionStatus")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string TransactionStatus { get; set; }

        // Related Transactions (optional one-to-optional one)
        public virtual ICollection<Transaction>? TransferTransactions { get; set; }
    }
}