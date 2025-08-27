using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TRKart.Entities.Enums;

namespace TRKart.Entities.Models
{
    [Table("CardUpdates")]
    public class CardUpdates
    {
        [Key]
        [Column("UpdateID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UpdateID { get; set; }

        [Required]
        [Column("CardID", TypeName = "INT")]
        [ForeignKey("UserCard")]
        public int CardID { get; set; }

        // Navigation property
        public UserCard Card { get; set; }

        [Column("PreviousStatus")]
        public CardStatus? PreviousStatus { get; set; }

        [Column("NewStatus")]
        public CardStatus? NewStatus { get; set; }

        [Column("StatusUpdatedAt")]
        public DateTime? StatusUpdatedAt { get; set; }

        [Column("PreviousType")]
        public CardType? PreviousType { get; set; }

        [Column("NewType")]
        public CardType? NewType { get; set; }

        [Column("TypeUpdatedAt")]
        public DateTime? TypeUpdatedAt { get; set; }
    }
}