using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("CardLimits")]
    public class CardLimits
    {
        [Key]
        [Column("LimitID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LimitID { get; set; }

        [Column("CardID", TypeName = "INT")]
        [ForeignKey("UserCard")]
        public int CardID { get; set; }

        // Navigation property
        public UserCard Card { get; set; }

        [Required]
        [Column("PayLimit", TypeName = "DECIMAL(18, 2)")]
        public decimal PayLimit { get; set; }

        [Required]
        [Column("PayMaxLimit", TypeName = "DECIMAL(18, 2)")]
        public decimal PayMaxLimit { get; set; }

        [Column("PayLimitUpdatedAt")]
        public DateTimeOffset? PayLimitUpdatedAt { get; set; } = null!;

        [Required]
        [Column("TransferLimit", TypeName = "DECIMAL(18, 2)")]
        public decimal TransferLimit { get; set; }

        [Required]
        [Column("TransferMaxLimit", TypeName = "DECIMAL(18, 2)")]
        public decimal TransferMaxLimit { get; set; }

        [Column("TransferLimitUpdatedAt")]
        public DateTimeOffset? TransferLimitUpdatedAt { get; set; } = null!;

        [Required]
        [Column("CreatedAt")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
