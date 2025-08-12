using System.Collections.Generic;
using System.Threading.Tasks;
using TRKart.Entities.DTOs;
using TRKart.Entities.Models;

namespace TRKart.Business.Interfaces
{
    public interface IUserCardService
    {
        /// <summary>
        /// Create a new card for a customer
        /// </summary>
        Task<UserCardResponseDto> CreateUserCardAsync(CreateUserCardDto createDto);
        
        /// <summary>
        /// Get all cards for a customer
        /// </summary>
        Task<List<UserCardResponseDto>> GetUserCardsByCustomerIdAsync(int customerId);
        
        /// <summary>
        /// Get a specific card by card number
        /// </summary>
        Task<UserCardResponseDto> GetUserCardByNumberAsync(string cardNumber);

        /// <summary>
        /// Get status change history for a card
        /// </summary>
        /// <param name="cardNumber">The card number to get history for</param>
        /// <returns>List of status changes, or null if card not found</returns>
        Task<List<CardStatusHistoryDto>> GetCardStatusHistoryAsync(string cardNumber);

        /// <summary>
        /// Updates the status of a user's card after verifying the password
        /// </summary>
        /// <param name="updateDto">Contains card ID, new status, and user's password for verification</param>
        /// <returns>True if update was successful, false otherwise</returns>
        Task<bool> UpdateCardStatusAsync(CardStatusUpdateDto updateDto);
    }
}