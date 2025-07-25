using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TRKart.Entities.DTOs
{
    // DTO for creating a new user card
    public class CreateUserCardDto
    {
        [Required(ErrorMessage = "CustomerID is required")]
        public int CustomerID { get; set; }
    }

    // DTO for deleting a user card
    public class DeleteUserCardDto
    {
        [Required(ErrorMessage = "CardNumber is required")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "CardNumber must be exactly 16 characters")]
        public string CardNumber { get; set; }
    }

    // DTO for returning user card information
    public class UserCardResponseDto
    {
        public int CardID { get; set; }
        public int CustomerID { get; set; }
        public string CardNumber { get; set; }
        public decimal Balance { get; set; }
        public string CardStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}