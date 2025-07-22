using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace TRKart.Entities
{
    [Table("usercards")] // Name of the table in the database
    public class UserCard
    {
        // Maps to CardId SERIAL PRIMARY KEY
        [Key] // Primary key annotation for Entity Framework Core
        public int CardId { get; set; }

        // Maps to CustomerId INT NOT NULL, acting as a Foreign Key
        public int CustomerId { get; set; }

        // Navigation property for the one-to-many relationship with Customer
        public Customer Customer { get; set; } = null!;

        // Maps to Balance DECIMAL(10, 2) NOT NULL DEFAULT 0.00
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Balance { get; set; }

        // Maps to CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        // Created by database upon insert
        public DateTime CreatedAt { get; set; }

        // Maps to CardNumber CHAR(16) NOT NULL UNIQUE
        // Default is created by database. 16 digit; numbers and uppercase letters only
        [Column(TypeName = "char(16)")]
        [Required]
        public string CardNumber { get; set; } = null!;

    }
}