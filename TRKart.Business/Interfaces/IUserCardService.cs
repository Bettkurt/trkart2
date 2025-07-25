using System.Collections.Generic;
using System.Threading.Tasks;
using TRKart.Entities.DTOs;

namespace TRKart.Business.Interfaces
{
    public interface IUserCardService
    {
        // Create a new card for a customer
        Task<UserCardResponseDto> CreateUserCardAsync(CreateUserCardDto createDto);
        
        // Delete a card by card number
        Task<bool> DeleteUserCardAsync(DeleteUserCardDto deleteDto);
        
        // Get all cards for a customer
        Task<List<UserCardResponseDto>> GetUserCardsByCustomerIdAsync(int customerId);
        
        // Get a specific card by card number
        Task<UserCardResponseDto> GetUserCardByNumberAsync(string cardNumber);
    }
}