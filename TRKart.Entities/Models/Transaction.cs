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
        public UserCard UserCard { get; set; }

        [Column("Amount")]
        public decimal Amount { get; set; }

        [Column("TransactionType")]
        public string TransactionType { get; set; }

        [Column("Description")]
        public string? Description { get; set; }

        [Column("TransactionDate")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime TransactionDate { get; set; }

        [Column("TransactionStatus")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? TransactionStatus { get; set; }
    }
} 