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

        [Column("AccessToken")]
        public string? AccessToken { get; set; }

        [Required]
        [Column("RefreshToken")]
        public string RefreshToken { get; set; }

        [Column("AccessTokenExpiration")]
        public DateTime? AccessTokenExpiration { get; set; }

        [Required]
        [Column("RefreshTokenExpiration")]
        public DateTime RefreshTokenExpiration { get; set; }

        [Required]
        [Column("CreatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedAt { get; set; }

        [Column("IsRevoked")]
        public bool IsRevoked { get; set; } = false;

        [Column("DeviceInfo")]
        public string? DeviceInfo { get; set; }

        [Column("IPAddress")]
        public string? IPAddress { get; set; }
    }
}