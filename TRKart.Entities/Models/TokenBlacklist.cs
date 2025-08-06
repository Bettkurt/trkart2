using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("TokenBlacklist")]
    public class TokenBlacklist
    {
        [Key]
        [Column("BlacklistID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BlacklistID { get; set; }

        [Required]
        [Column("RefreshToken")]
        public string RefreshToken { get; set; }

        [Required]
        [Column("BlacklistedAt")]
        public DateTime BlacklistedAt { get; set; }

        [Column("Reason")]
        public string? Reason { get; set; }

        [Column("IPAddress")]
        public string? IPAddress { get; set; }
    }
}
