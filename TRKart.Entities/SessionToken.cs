using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities
{
    [Table("SessionToken")]
    public class SessionToken
    {
        [Key]
        public int SessionID { get; set; }

        [ForeignKey("CustomerID")]
        public int CustomerID { get; set; }
        public virtual Customers Customer { get; set; } = null!;

        [Required]
        public string Token { get; set; }

        public DateTime Expiration { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
