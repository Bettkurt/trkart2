using System;

namespace TRKart.Entities
{
    public class SessionToken
    {
        public int Id { get; set; }

        public string Token { get; set; } = null!;

        public DateTime Expiration { get; set; }  // Bu tek yeterli

        public int CustomerId { get; set; }       // Foreign Key

        public Customer Customer { get; set; } = null!;  // Navigation Property
    }
}
