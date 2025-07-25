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
        public int TransactionID { get; set; }

        [Column("CardID")]
        public int CardID { get; set; }

        [Column("Amount")]
        public decimal Amount { get; set; }

        [Column("TransactionType")]
        public string TransactionType { get; set; } = null!;

        [Column("Description")]
        public string? Description { get; set; }

        [Column("TransactionDate")]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Column("TransactionStatus")]
        public string? TransactionStatus { get; set; } = "Completed";
    }
} 