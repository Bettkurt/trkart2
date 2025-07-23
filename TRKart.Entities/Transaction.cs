using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TRKart.Entities
{
    public class Transaction
    {
        public int TransactionID { get; set; }
        public int CardID { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionStatus { get; set; } = "Completed";

        public UserCard? UserCard { get; set; }
    }
} 