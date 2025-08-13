using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TRKart.Entities.DTOs
{
    public class UserCardDto
    {
        public int CardID { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string CardStatus { get; set; } = string.Empty;
        public int CustomerID { get; set; }
    }

    // DTO for creating a new user card
    public class CreateUserCardDto
    {
        [Required(ErrorMessage = "CustomerID is required")]
        public int CustomerID { get; set; }

        [StringLength(20, ErrorMessage = "Card name cannot exceed 20 characters")]
        public string? CardName { get; set; }
    }

    public class CardStatusUpdateDto
    {
        /// <summary>
        /// The ID of the card to update
        /// </summary>
        [Required(ErrorMessage = "Card ID is required")]
        public int CardId { get; set; }

        /// <summary>
        /// The new status to set for the card (e.g., 'Deactivated', 'Active', 'Lost')
        /// </summary>
        [Required(ErrorMessage = "Status is required")]
        [StringLength(20, ErrorMessage = "Status cannot be longer than 20 characters")]
        public string Status { get; set; }
    }

    // DTO for returning user card information
    public class UserCardResponseDto
    {
        public int CardID { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public int CustomerID { get; set; }
        public decimal Balance { get; set; }
        public string CardStatus { get; set; } = string.Empty;
        public string? CardName { get; set; }
        public DateTime CardExpirationDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    // DTO for card status change history
    public class CardStatusHistoryDto
    {
        public int UpdateID { get; set; }
        public int CardID { get; set; }
        public string PreviousStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}