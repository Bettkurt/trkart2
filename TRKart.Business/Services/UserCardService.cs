using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TRKart.Business.Interfaces;
using TRKart.Core.Helpers;
using TRKart.Core.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;
using TRKart.Entities.Enums;

namespace TRKart.Business.Services
{
    public class UserCardService : IUserCardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUniqueNumberChecker _uniqueNumberChecker;
        private readonly ILogger<UserCardService> _logger;

        public UserCardService(ApplicationDbContext context, 
                             IUniqueNumberChecker uniqueNumberChecker,
                             ILogger<UserCardService> logger,
                             IUniqueNumberChecker uniqueNumberChecker)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _uniqueNumberChecker = uniqueNumberChecker ?? throw new ArgumentNullException(nameof(uniqueNumberChecker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserCardResponseDto> CreateUserCardAsync(CreateUserCardDto createDto)
        {
            if (createDto == null)
                throw new ArgumentNullException(nameof(createDto));

            _logger.LogInformation("Starting to create a new UserCard for CustomerID: {CustomerID}", createDto.CustomerID);

            // First, verify the customer exists
            var customerExists = await _context.Customers.AnyAsync(c => c.CustomerID == createDto.CustomerID);
            if (!customerExists)
            {
                _logger.LogWarning("Customer with ID {CustomerID} not found", createDto.CustomerID);
                throw new KeyNotFoundException($"Customer with ID {createDto.CustomerID} not found");
            }

            // Generate card number with TRK prefix and proper validation
            var cardNumber = await CardNumberHelper.GenerateCardNumberAsync(_uniqueNumberChecker);
            // Set by DB
            //a var expirationDate = DateTime.UtcNow.AddYears(5).AddMonths(1).AddDays(-1);
            
            var newCard = new UserCard
            {
                CustomerID = createDto.CustomerID,
                CardNumber = cardNumber,
                // Balance, // Default balance is set to 0.00 by DB
                CardStatus = CardStatus.Inactive, // Default status
                CardType = createDto.CardType,
                CardName = createDto.CardName,
                // Set by DB
                //a CardExpirationDate = expirationDate,
                //a CreatedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogDebug("Adding new UserCard to context");
                await _context.UserCard.AddAsync(newCard);
                _logger.LogDebug("Saving changes to database");
                await _context.SaveChangesAsync();

                int recordsAffected = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChanges completed. Records affected: {RecordsAffected}", recordsAffected);
                
                if (newCard.CardID <= 0)
                {
                    _logger.LogWarning("CardID was not set after SaveChanges. This might indicate an issue with the database operation.");
                    throw new InvalidOperationException("Failed to create user card. CardID is not set.");
                }

                // Explicitly reload the entity to ensure we have all database-generated values
                _logger.LogDebug("Reloading UserCard entity to get database-generated values");
                await _context.Entry(newCard).ReloadAsync();

                if (string.IsNullOrEmpty(newCard.CardNumber))
                {
                    _logger.LogError("CardNumber is still null after reloading the entity. This indicates a problem with the database trigger or default value.");
                    throw new InvalidOperationException("Failed to create user card. CardNumber is not set.");
                }

                var result = MapToResponseDto(newCard);
                _logger.LogInformation("Successfully created UserCard with CardID: {CardID}, CardNumber: {CardNumber}",                 
                result.CardID, result.CardNumber);
                
                return result;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while creating UserCard. Inner exception: {InnerException}", dbEx.InnerException?.Message);
                throw new InvalidOperationException("A database error occurred while creating the user card.", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating UserCard. Error: {ErrorMessage}", ex.Message);
                throw new InvalidOperationException("An unexpected error occurred while creating the user card.", ex);
            }
        }

        public async Task<bool> UpdateCardStatusAsync(CardStatusUpdateDto updateDto)
        {
            if (updateDto == null)
                throw new ArgumentNullException(nameof(updateDto));

            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                Console.WriteLine($"[UpdateCardStatusAsync] Starting update for card ID: {updateDto.CardID}");
                Console.WriteLine($"[UpdateCardStatusAsync] New status: {updateDto.Status}");

                // Find the card by ID with tracking
                var card = await _context.UserCard
                    .Include(c => c.Customer) // Include customer for blacklist
                    .FirstOrDefaultAsync(c => c.CardID == updateDto.CardID);

                if (card == null)
                {
                    Console.WriteLine($"[UpdateCardStatusAsync] Card with ID {updateDto.CardID} not found");
                    return false;
                }

                Console.WriteLine($"[UpdateCardStatusAsync] Found card - Old status: {card.CardStatus}");

                // Log the current status before update
                Console.WriteLine($"[UpdateCardStatusAsync] Current status: {card.CardStatus}, New status: {updateDto.Status}");

                // Check if status is actually changing
                if (card.CardStatus == updateDto.Status)
                {
                    Console.WriteLine($"[UpdateCardStatusAsync] Status is already {updateDto.Status}, no update needed");
                    return true;
                }

                // Save the old status for logging and blacklist check
                //a var oldStatus = card.CardStatus;
                
                // Update the card status first
                card.CardStatus = updateDto.Status;
                
                // Create status update record with explicit UTC timestamps
                //a var utcNow = DateTime.UtcNow;

                // DB handles CardUpdates entry creations
                /* var statusUpdate = new CardUpdates
                {
                    CardID = card.CardID,
                    PreviousStatus = oldStatus,
                    NewStatus = updateDto.Status,
                    StatusUpdatedAt = utcNow,
                    //a PreviousType = card.CardType,
                    //a NewType = card.CardType,
                    //a TypeUpdatedAt = utcNow,
                };
                _context.CardUpdates.Add(statusUpdate); */
                
                // Check if we need to blacklist the card
                // Expired (1) card blacklistings are handled by background services
                // Only blacklist if the new status is Deactivated (0) or Lost (2)
                bool shouldBlacklist = (updateDto.Status == CardStatus.Deactivated || updateDto.Status == CardStatus.Lost);
                
                Console.WriteLine($"[UpdateCardStatusAsync] Should blacklist: {shouldBlacklist}");
                
                // Add to blacklist if needed
                if (shouldBlacklist)
                {
                    Console.WriteLine($"[UpdateCardStatusAsync] Attempting to blacklist card {card.CardNumber}");
                    
                    // Check if not already blacklisted
                    bool isAlreadyBlacklisted = await _context.CardBlacklist
                        .AnyAsync(cb => cb.OriginalCardID == card.CardID);
                        
                    Console.WriteLine($"[UpdateCardStatusAsync] Already blacklisted: {isAlreadyBlacklisted}");
                    
                    if (!isAlreadyBlacklisted)
                    {
                        var blacklistReason = updateDto.Status; // 0: Deactivated, 1: Expired, 2: Lost
                        
                        Console.WriteLine($"[UpdateCardStatusAsync] Creating blacklist entry with reason: {blacklistReason}");
                        
                        // Create blacklist entry with the new status
                        await CreateCardBlacklistAsync(card, (int)updateDto.Status);
                        
                        Console.WriteLine($"[UpdateCardStatusAsync] Added card {card.CardNumber} to blacklist");
                    }
                }

                try
                {
                    var changes = await _context.SaveChangesAsync();
                    Console.WriteLine($"[UpdateCardStatusAsync] Changes saved. Rows affected: {changes}");
                    
                    await transaction.CommitAsync();
                    Console.WriteLine($"[UpdateCardStatusAsync] Transaction committed successfully");
                    
                    return true;
                }
                catch (DbUpdateException dbEx)
                {
                    Console.WriteLine($"[UpdateCardStatusAsync] Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                    await transaction.RollbackAsync();
                    Console.WriteLine($"[UpdateCardStatusAsync] Transaction rolled back due to error");
                    throw;
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error updating card status for card ID: {CardId}", updateDto?.CardId);
                await transaction.RollbackAsync();
                Console.WriteLine($"[UpdateCardStatusAsync] Error updating card status: {ex.Message}");
                return false;
            }
        }

        public async Task<List<UserCardResponseDto>> GetUserCardsByCustomerIdAsync(int customerId)
        {
            return await _context.UserCard
                // 0 = Deactivated (It is deleted from user's perspective), 1 = Expired 
                // We show cards that are not deactivated or expired
                .Where(c => c.CustomerID == customerId && c.CardStatus > CardStatus.Expired)
                .Select(c => MapToResponseDto(c))
                .ToListAsync();
        }

        public async Task<UserCardResponseDto> GetUserCardByNumberAsync(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                throw new ArgumentException("Card number cannot be empty", nameof(cardNumber));

            var card = await _context.UserCard
                // 0 = Deactivated (It is deleted from user's perspective), 1 = Expired 
                // We show cards that are not deactivated or expired
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber && c.CardStatus > CardStatus.Expired);

            return card != null ? MapToResponseDto(card) : null;
        }

        public async Task<List<CardStatusHistoryDto>> GetCardStatusHistoryAsync(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                throw new ArgumentException("Card number cannot be empty", nameof(cardNumber));

            // First get the card ID, excluding deactivated and expired cards
            var card = await _context.UserCard
                .Where(c => c.CardNumber == cardNumber && c.CardStatus > CardStatus.Expired)
                .Select(c => new { c.CardID })
                .FirstOrDefaultAsync();

            if (card == null)
                return null;

            return await _context.CardUpdates
                .Where(cu => cu.CardID == card.CardID)
                .OrderByDescending(cu => cu.StatusUpdatedAt)
                .Select(cu => new CardStatusHistoryDto
                {
                    UpdateID = cu.UpdateID,
                    CardID = cu.CardID,
                    PreviousStatus = (CardStatus)cu.PreviousStatus,
                    NewStatus = (CardStatus)cu.NewStatus,
                    StatusUpdatedAt = cu.StatusUpdatedAt ?? DateTime.UtcNow
                })
                .ToListAsync();
        }

        private async Task CreateCardBlacklistAsync(UserCard card, int status)
        {
            // Check if card is already blacklisted
            var existingBlacklist = await _context.CardBlacklist
                .FirstOrDefaultAsync(cb => cb.OriginalCardID == card.CardID);
                
            if (existingBlacklist != null)
            {
                Console.WriteLine($"[CreateCardBlacklistAsync] Card {card.CardID} is already blacklisted");
                return;
            }
            
            // Map status to reason
            string reason = status switch
            {
                0 => "Card deactivated",
                1 => "Card expired",
                2 => "Card reported lost/stolen",
                _ => "Unknown reason"
            };
            
            // Ensure all DateTime values are properly specified as UTC
            //a var utcNow = DateTime.UtcNow;
            var cardExpirationDate = card.CardExpirationDate.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(card.CardExpirationDate, DateTimeKind.Utc)
                : card.CardExpirationDate.ToUniversalTime();
                
            var originalCreatedAt = card.CreatedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(card.CreatedAt, DateTimeKind.Utc)
                : card.CreatedAt.ToUniversalTime();

            var blacklist = new CardBlacklist
            {
                CustomerID = card.CustomerID,
                OriginalCardID = card.CardID,
                CardNumber = card.CardNumber,
                CardType = card.CardType,
                LeftOverBalance = card.Balance,
                CardExpirationDate = cardExpirationDate,
                OriginalCreatedAt = originalCreatedAt,
                Reason = (CardBlacklistReason)status,
                Notes = $"Automatically blacklisted: {reason}"
            };
            
            await _context.CardBlacklist.AddAsync(blacklist);
            Console.WriteLine($"[CreateCardBlacklistAsync] Created blacklist entry for card {card.CardID} with reason: {reason}");
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
                CardType = card.CardType,
                CardName = card.CardName,
                CardExpirationDate = card.CardExpirationDate,
            };
        }
    }
}