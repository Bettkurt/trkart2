using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TRKart.Entities.Models;

namespace TRKart.Entities.Models
{
    [Table("UserCard")] // Name of the table in the database
    public class UserCard
    {
        // Maps to CardId SERIAL PRIMARY KEY
        [Key]
        [Column("CardID", TypeName = "int")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CardID { get; set; }

        // Maps to CardNumber CHAR(16) NOT NULL UNIQUE
        // Default is created by database. 16 digit; numbers and uppercase letters only
        [Column("CardNumber", TypeName = "char(16)")]
        public string CardNumber { get; set; }

        // Maps to CustomerId INT NOT NULL, acting as a Foreign Key
        [Column("CustomerID", TypeName = "int")]
        [ForeignKey("Customer")]
        public int CustomerID { get; set; }
        // Navigation property for the one-to-many relationship with Customer
        public Customers Customer { get; set; }

        // Maps to Balance DECIMAL(10, 2) NOT NULL DEFAULT 0.00
        [Column("Balance", TypeName = "decimal(10, 2)")]
        public decimal Balance { get; set; }

        // Maps to CardStatus VARCHAR(20) NOT NULL DEFAULT 'Inactive'
        [Column("CardStatus", TypeName = "varchar(20)")]
        public string CardStatus { get; set; }

        // Maps to CardName VARCHAR(20)
        [Column("CardName", TypeName = "varchar(20)")]
        public string? CardName { get; set; } = null!;

        // Maps to CardExpirationDate DATE NOT NULL DEFAULT (DATE_TRUNC('MONTH', CURRENT_DATE) + INTERVAL '5 years' + INTERVAL '1 month - 1 day')::DATE
        [Column("CardExpirationDate", TypeName = "date")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CardExpirationDate { get; set; }

        // Maps to CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        [Column("CreatedAt", TypeName = "timestamp")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedAt { get; set; }

        [Column("LastUpdate", TypeName = "timestamp")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime LastUpdate { get; set; }

        // Related Transactions (optional one-to-optional many)
        public ICollection<Transaction>? Transactions { get; set; }

        // Related CardUpdates (optional one-to-optional many)
        public ICollection<CardUpdates>? CardUpdates { get; set; }
    }
}