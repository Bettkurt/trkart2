using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRKart.Entities
{
    public class SessionToken
    {
        public int Id { get; set; }

        public string Token { get; set; } = null!;

        public DateTime ExpiryDate { get; set; }

        public int CustomerId { get; set; } 
       
        public object? Customer { get; set; }
        public DateTime Expiration { get; set; }
    }
}

