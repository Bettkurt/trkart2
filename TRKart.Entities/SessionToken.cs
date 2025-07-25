using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities
{
    [Table("SessionToken")]
    public class SessionToken
    {
        [Key]
        [Column("SessionID")]
        public int SessionID { get; set; }

        [Column("CustomerID")]
        public int CustomerID { get; set; }

        [Column("Token")]
        public string Token { get; set; } = null!;

        [Column("Expiration")]
        public string Expiration { get; set; } = null!;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
