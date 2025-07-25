using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities
{
    [Table("UserCard")]
    public class UserCard
    {
        [Key]
        [Column("CardID")]
        public int CardID { get; set; }

        [Column("CustomerID")]
        public int CustomerID { get; set; }

        [Column("CardNumber")]
        public string CardNumber { get; set; } = null!;

        [Column("Balance")]
        public decimal Balance { get; set; } = 0.00m;

        [Column("CardStatus")]
        public string CardStatus { get; set; } = "Active";

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 