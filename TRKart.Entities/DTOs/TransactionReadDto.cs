using System;

namespace TRKart.Entities.DTOs
{
    public class TransactionReadDto
    {
        public int TransactionID { get; set; }
        public int CardID { get; set; }
        public string CardNumber { get; set; } = null!;
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? TransactionStatus { get; set; }
    }
} 