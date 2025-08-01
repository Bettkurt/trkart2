using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TRKart.Entities.Models
{
    [Table("UserCard")] // Name of the table in the database
    public class UserCard
    {
        // Maps to CardId SERIAL PRIMARY KEY
        [Key]
        [Column("CardID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CardID { get; set; }

        // Maps to CardNumber CHAR(16) NOT NULL UNIQUE
        // Default is created by database. 16 digit; numbers and uppercase letters only
        [Column(TypeName = "char(16)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string CardNumber { get; set; }

        // Maps to CustomerId INT NOT NULL, acting as a Foreign Key
        [Column("CustomerID")]
        [ForeignKey("Customer")]
        public int CustomerID { get; set; }
        // Navigation property for the one-to-many relationship with Customer
        public Customers Customer { get; set; }

        // Maps to Balance DECIMAL(10, 2) NOT NULL DEFAULT 0.00
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Balance { get; set; }

        // Maps to CardStatus VARCHAR(20) NOT NULL DEFAULT 'Inactive'
        [Column(TypeName = "varchar(20)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string CardStatus { get; set; }

        // Maps to CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedAt { get; set; }

        // Related Transactions (mandatory one-to-optional many)
        public ICollection<Transaction>? Transactions { get; set; }
    }
}