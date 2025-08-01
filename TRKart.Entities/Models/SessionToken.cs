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

        [Column("CustomerID")]
        [ForeignKey("Customer")]
        public int CustomerID { get; set; }
        // Navigation property for the one-to-many relationship with Customer
        public Customers Customer { get; set; }

        [Required]
        [Column("Token")]
        public string Token { get; set; }

        [Required]
        [Column("ExpirationDate")]
        public DateTime ExpirationDate { get; set; }

        [Required]
        [Column("CreatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedAt { get; set; }
    }
}