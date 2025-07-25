using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities
{
    [Table("Customers")]          // Table name
    public class Customers
    {
        [Key]                     // Primary Key
        [Column("CustomerID")]
        public int CustomerID { get; set; }

        [Column("FullName")]
        public string FullName  { get; set; } = null!;

        [Column("Email")]
        public string Email { get; set; }

        [Column("PasswordHash")]
        public string PasswordHash { get; set; } = null!;

        // Related SessionTokens
        public ICollection<SessionToken> SessionTokens { get; set; } = new List<SessionToken>();

        // Related UserCards
        public ICollection<UserCard> UserCards { get; set; } = new List<UserCard>();
    }
}