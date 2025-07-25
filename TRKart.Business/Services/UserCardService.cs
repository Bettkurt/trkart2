using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities;
using TRKart.Entities.DTOs;

namespace TRKart.Business.Services
{
    public class UserCardService : IUserCardService
    {
        private readonly ApplicationDbContext _context;

        public UserCardService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
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

            var newCard = new UserCard
            {
                CustomerID = createDto.CustomerID,
                // Let the database handle the default values for CardNumber, Balance, CardStatus, and CreatedAt
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

        public async Task<bool> DeleteUserCardAsync(DeleteUserCardDto deleteDto)
        {
            if (deleteDto == null)
                throw new ArgumentNullException(nameof(deleteDto));

            var card = await _context.UserCard
                .FirstOrDefaultAsync(c => c.CardNumber == deleteDto.CardNumber);

            if (card == null)
                return false;

            _context.UserCard.Remove(card);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<UserCardResponseDto>> GetUserCardsByCustomerIdAsync(int customerID)
        {
            return await _context.UserCard
                .Where(c => c.CustomerID == customerID)
                .Select(c => MapToResponseDto(c))
                .ToListAsync();
        }

        public async Task<UserCardResponseDto> GetUserCardByNumberAsync(string CardNumber)
        {
            if (string.IsNullOrWhiteSpace(CardNumber))
                throw new ArgumentException("Card number cannot be empty", nameof(CardNumber));

            var card = await _context.UserCard
                .FirstOrDefaultAsync(c => c.CardNumber == CardNumber);

            return card != null ? MapToResponseDto(card) : null;
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
                CreatedAt = card.CreatedAt
            };
        }
    }
}