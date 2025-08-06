using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("Customers")]          // Table name
    public class Customers
    {
        [Key]                     // Primary Key
        [Column("CustomerID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerID { get; set; }

        [Column("CustomerNumber")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string CustomerNumber { get; set; }

        [Column("FullName")]
        public string FullName  { get; set; } = null!;

        [Column("Email")]
        [Required]
        public string Email { get; set; }

        [Column("VerifiedUser")]
        [Required]
        public bool VerifiedUser { get; set; } = true;

        [Column("PasswordHash")]
        [Required]
        public string PasswordHash { get; set; }

        // Related SessionTokens (mandatory one-to-mandatory many)
        public virtual ICollection<SessionToken> SessionTokens { get; set; } = new List<SessionToken>();

        // Related UserCards (mandatory one-to-optional many)
        public virtual ICollection<UserCard>? UserCards { get; set; } = new List<UserCard>();
    }
}