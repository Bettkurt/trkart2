using System;

namespace TRKart.Entities.DTOs
{
    public class WalletLoadResponseDto
    {
        public int TransactionId { get; set; }
        public int WalletId { get; set; }
        public int? CardId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; }
        public string Description { get; set; }
        public string ReferenceId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; }
        public decimal NewBalance { get; set; }
    }
}
