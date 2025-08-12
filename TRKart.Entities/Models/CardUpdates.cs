using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("CardUpdates")]
    public class CardUpdates
    {
        [Key]
        [Column("UpdateID", TypeName = "int")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UpdateID { get; set; }

        [Required]
        [Column("CardID", TypeName = "int")]
        [ForeignKey("UserCard")]
        public int CardID { get; set; }

        // Navigation property
        public UserCard Card { get; set; }

        [Required]
        [Column("PreviousStatus", TypeName = "varchar(20)")]
        public string PreviousStatus { get; set; }

        [Required]
        [Column("NewStatus", TypeName = "varchar(20)")]
        public string NewStatus { get; set; }

        [Required]
        [Column("UpdatedAt", TypeName = "timestamp")]
        public DateTime UpdatedAt { get; set; }
    }
}

