using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TRKart.Business.Interfaces;
using TRKart.Core.Helpers;
using TRKart.Core.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;

namespace TRKart.Business.Services
{
    public class UserCardService : IUserCardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUniqueNumberChecker _uniqueNumberChecker;

        public UserCardService(ApplicationDbContext context, 
                             IUniqueNumberChecker uniqueNumberChecker)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _uniqueNumberChecker = uniqueNumberChecker ?? throw new ArgumentNullException(nameof(uniqueNumberChecker));
        }

        public async Task<UserCardResponseDto> CreateUserCardAsync(CreateUserCardDto createDto)
        {
            if (createDto == null)
                throw new ArgumentNullException(nameof(createDto));

            // _logger.LogInformation("Starting to create a new UserCard for CustomerID: {CustomerID}", createDto.CustomerID);

            // First, verify the customer exists
            var customerExists = await _context.Customers.AnyAsync(c => c.CustomerID == createDto.CustomerID);
            if (!customerExists)
            {
                // _logger.LogWarning("Customer with ID {CustomerID} not found", createDto.CustomerID);
                throw new KeyNotFoundException($"Customer with ID {createDto.CustomerID} not found");
            }

            // Generate card number with TRK prefix and proper validation
            var cardNumber = await CardNumberHelper.GenerateCardNumberAsync(_uniqueNumberChecker);
            //var expirationDate = DateTime.UtcNow.AddYears(5).AddMonths(1).AddDays(-1);
            
            var newCard = new UserCard
            {
                CustomerID = createDto.CustomerID,
                CardNumber = cardNumber,
                Balance = 0, // Default balance
                CardStatus = "Inactive", // Default status
                //CardExpirationDate = expirationDate,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                // _logger.LogDebug("Adding new UserCard to context");
                await _context.UserCard.AddAsync(newCard);
                // _logger.LogDebug("Saving changes to database");
                
                int recordsAffected = await _context.SaveChangesAsync();
                // _logger.LogInformation("SaveChanges completed. Records affected: {RecordsAffected}", recordsAffected);
                
                if (newCard.CardID <= 0)
                {
                    // _logger.LogWarning("CardID was not set after SaveChanges. This might indicate an issue with the database operation.");
                }

                // Explicitly reload the entity to ensure we have all database-generated values
                // _logger.LogDebug("Reloading UserCard entity to get database-generated values");
                await _context.Entry(newCard).ReloadAsync();

                if (string.IsNullOrEmpty(newCard.CardNumber))
                {
                    // _logger.LogError("CardNumber is still null after reloading the entity. This indicates a problem with the database trigger or default value.");
                }

                var result = MapToResponseDto(newCard);
                // _logger.LogInformation("Successfully created UserCard with CardID: {CardID}, CardNumber: {CardNumber}", 
                //     result.CardID, result.CardNumber);
                
                return result;
            }
            catch (DbUpdateException dbEx)
            {
                // _logger.LogError(dbEx, "Database error while creating UserCard. Inner exception: {InnerException}", dbEx.InnerException?.Message);
                throw new InvalidOperationException("A database error occurred while creating the user card.", dbEx);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Unexpected error creating UserCard. Error: {ErrorMessage}", ex.Message);
                throw new InvalidOperationException("An unexpected error occurred while creating the user card.", ex);
            }
        }

        public async Task<bool> UpdateCardStatusAsync(CardStatusUpdateDto updateDto)
        {
            if (updateDto == null)
                throw new ArgumentNullException(nameof(updateDto));

            try
            {
                Console.WriteLine($"[UpdateCardStatusAsync] Starting update for card ID: {updateDto.CardId}");
                Console.WriteLine($"[UpdateCardStatusAsync] New status: {updateDto.Status}");

                // Find the card by ID
                var card = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardID == updateDto.CardId);

                if (card == null)
                {
                    Console.WriteLine($"[UpdateCardStatusAsync] Card with ID {updateDto.CardId} not found");
                    return false;
                }

                Console.WriteLine($"[UpdateCardStatusAsync] Found card - Old status: {card.CardStatus}");

                // Update the card status
                card.CardStatus = updateDto.Status;
                //card.LastUpdate = DateTime.UtcNow;

                Console.WriteLine($"[UpdateCardStatusAsync] Saving changes to database...");
                
                // Save changes - the CardUpdates table is updated automatically by a database trigger
                int changes = await _context.SaveChangesAsync();
                
                Console.WriteLine($"[UpdateCardStatusAsync] Changes saved. Rows affected: {changes}");
                Console.WriteLine($"[UpdateCardStatusAsync] Card status updated successfully");

                return true;
            }
            catch (Exception ex)
            {
                // Log the error
                // _logger.LogError(ex, "Error updating card status for card ID: {CardId}", updateDto?.CardId);
                return false;
            }
        }

        public async Task<List<UserCardResponseDto>> GetUserCardsByCustomerIdAsync(int customerID)
        {
            return await _context.UserCard
                .Where(c => c.CustomerID == customerID && c.CardStatus != "Deactivated")
                .Select(c => MapToResponseDto(c))
                .ToListAsync();
        }

        public async Task<UserCardResponseDto> GetUserCardByNumberAsync(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                throw new ArgumentException("Card number cannot be empty", nameof(cardNumber));

            var card = await _context.UserCard
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber && c.CardStatus != "Deactivated");

            return card != null ? MapToResponseDto(card) : null;
        }

        public async Task<List<CardStatusHistoryDto>> GetCardStatusHistoryAsync(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                throw new ArgumentException("Card number cannot be empty", nameof(cardNumber));

            // First get the card ID, excluding deactivated cards
            var card = await _context.UserCard
                .Where(c => c.CardNumber == cardNumber && c.CardStatus != "Deactivated")
                .Select(c => new { c.CardID })
                .FirstOrDefaultAsync();

            if (card == null)
                return null;

            return await _context.CardUpdates
                .Where(cu => cu.CardID == card.CardID)
                .OrderByDescending(cu => cu.UpdatedAt)
                .Select(cu => new CardStatusHistoryDto
                {
                    UpdateID = cu.UpdateID,
                    CardID = cu.CardID,
                    PreviousStatus = cu.PreviousStatus,
                    NewStatus = cu.NewStatus,
                    UpdatedAt = cu.UpdatedAt
                })
                .ToListAsync();
        }

        private static UserCardResponseDto MapToResponseDto(UserCard card)
        {
            if (card == null) return null;

            return new UserCardResponseDto
            {
                CardID = card.CardID,
                CustomerID = card.CustomerID,
                CardNumber = card.CardNumber,
                Balance = card.Balance,
                CardStatus = card.CardStatus,
                CardName = card.CardName,
                CardExpirationDate = card.CardExpirationDate,
                CreatedAt = card.CreatedAt,
                LastUpdate = card.LastUpdate
            };
        }
    }
}