using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities
{
    [Table("customers")]          // (İsteğe bağlı) tablo adını da sabitle
    public class Customer
    {
        [Key]                     // Birincil anahtar
        [Column("id")]
        public int Id { get; set; }

       [Column("email")]
    public string Email { get; set; } = null!;
        [Column("passwordhash")]
        public string PasswordHash { get; set; } = null!;

        [Column("fullname")]
        public string FullName  { get; set; } = null!;

        // İlişkili SessionToken’lar
        public ICollection<SessionToken> SessionTokens { get; set; } = new List<SessionToken>();
    }
}
