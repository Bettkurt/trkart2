using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TRKart.Entities.Enums;

namespace TRKart.Entities.DTOs
{
    public class UserCardDto
    {
        public int CardID { get; set; }
        public int CustomerID { get; set; }
        public string CardNumber { get; set; }
        public decimal Balance { get; set; }
        public CardStatus CardStatus { get; set; }
        public CardType CardType { get; set; }
        public string? CardName { get; set; }
    }

    // DTO for creating a new user card
    public class CreateUserCardDto
    {
        [Required(ErrorMessage = "CustomerID is required")]
        public int CustomerID { get; set; }

        [Required(ErrorMessage = "CardType is required")]
        [EnumDataType(typeof(CardType), ErrorMessage = "Invalid card type")]
        public CardType CardType { get; set; } = CardType.Standard;

        [StringLength(16, ErrorMessage = "Card name cannot exceed 16 characters")]
        public string? CardName { get; set; }
    }

    public class CardStatusUpdateDto
    {
        /// <summary>
        /// The ID of the card to update
        /// </summary>
        [Required(ErrorMessage = "Card ID is required")]
        public int CardID { get; set; }

        /// <summary>
        /// The new status to set for the card (e.g., 'Deactivated', 'Active', 'Lost')
        /// </summary>
        [Required(ErrorMessage = "Status is required")]
        [EnumDataType(typeof(CardStatus), ErrorMessage = "Invalid status value")]
        public CardStatus Status { get; set; }
    }

    // DTO for returning user card information
    public class UserCardResponseDto
    {
        public int CardID { get; set; }
        public int CustomerID { get; set; }
        public string CardNumber { get; set; }
        public decimal Balance { get; set; }
        public CardStatus CardStatus { get; set; }
        public CardType CardType { get; set; }
        public string? CardName { get; set; }
        public DateTime CardExpirationDate { get; set; }
    }

    // DTO for card status change history
    public class CardStatusHistoryDto
    {
        public int UpdateID { get; set; }
        public int CardID { get; set; }
        public CardStatus? PreviousStatus { get; set; }
        public CardStatus? NewStatus { get; set; }
        public DateTime? StatusUpdatedAt { get; set; }
    }

    // DTO for updating card name
    public class UpdateCardNameDto
    {
        /// <summary>
        /// The ID of the card to update
        /// </summary>
        [Required(ErrorMessage = "Card ID is required")]
        public int CardID { get; set; }

        /// <summary>
        /// The new name for the card
        /// </summary>
        [StringLength(16, ErrorMessage = "Card name cannot exceed 16 characters")]
        public string? CardName { get; set; }
    }
}