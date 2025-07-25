using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("SessionToken")]
    public class SessionToken
    {
        [Key]
        [Column("SessionID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SessionID { get; set; }

        [ForeignKey("CustomerID")]
        public int CustomerID { get; set; }
        public virtual Customers Customer { get; set; } = null!;

        [Required]
        [Column("Token")]
        public string Token { get; set; }

        [Required]
        [Column("Expiration")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime Expiration { get; set; }

        [Required]
        [Column("CreatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedAt { get; set; }
    }
}
