using System;

namespace TRKart.Entities
{
    public class UserCard
    {
        public int CardID { get; set; }
        public int CustomerID { get; set; }
        public string CardNumber { get; set; } = null!;
        public string ExpiryDate { get; set; } = null!;
        public string CardType { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

        public Customer? Customer { get; set; }
    }
} 