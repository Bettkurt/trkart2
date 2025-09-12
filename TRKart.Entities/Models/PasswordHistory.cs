using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("PasswordHistory")]
    public class PasswordHistory
    {
        [Key]
        [Column("PasswordHistoryID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PasswordHistoryID { get; set; }

        [Column("CustomerID")]
        [ForeignKey("Customer")]
        public int CustomerID { get; set; }

        [Column("PasswordHash")]
        [Required]
        public string PasswordHash { get; set; }

        [Column("CreatedAt")]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset CreatedAt{ get; set; }

        [Column("CreatedBy")]
        [Required]
        public string CreatedBy { get; set; } = "System";

        // Navigation property to Customer
        public Customers Customer { get; set; } 
    }
}