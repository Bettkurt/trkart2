using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace TRKart.Entities
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public ICollection<SessionToken> SessionTokens { get; set; } = new List<SessionToken>();
        public ICollection<UserCard> UserCards { get; set; } = new List<UserCard>();

    }
}

