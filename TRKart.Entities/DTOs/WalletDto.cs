using System;
using TRKart.Entities.Enums;

namespace TRKart.Entities.DTOs
{
    public class WalletDto
    {
        public int WalletId { get; set; }
        public int CustomerId { get; set; }
        public string WalletNumber { get; set; }
        public decimal Balance { get; set; }
        public CardStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateWalletDto
    {
        public int CustomerId { get; set; }
        public decimal InitialBalance { get; set; } = 0.00m;
    }
}
