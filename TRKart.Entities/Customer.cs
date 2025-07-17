using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRKart.Entities
{
    public class Customer
    {
        public int Id { get; set; }

        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public ICollection<SessionToken> SessionTokens { get; set; } = new List<SessionToken>();

    }
}

