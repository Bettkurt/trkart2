using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.Models;
using TRKart.Entities.Enums;

namespace TRKart.Business.Services
{
    public class CardExpirationService : ICardExpirationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CardExpirationService> _logger;

        public CardExpirationService(
            ApplicationDbContext context,
            ILogger<CardExpirationService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task CheckAndUpdateExpiredCardsAsync()
        {
            try
            {
                _logger.LogInformation("Starting expired cards check at {UtcNow}", DateTime.UtcNow);

                // Get current date
                var currentDate = DateTime.UtcNow.Date;
                
                var expiredCards = await _context.UserCard
                    .Where(c => c.CardExpirationDate < currentDate && 
                              c.CardStatus > CardStatus.Expired) // Only cards that are NOT already expired OR deactivated
                    .ToListAsync();

                if (!expiredCards.Any())
                {
                    _logger.LogInformation("No expired cards found");
                    return;
                }

                _logger.LogInformation("Found {Count} expired cards to update", expiredCards.Count);

                foreach (var card in expiredCards)
                {
                    try
                    {
                        // Create a transaction for each card to ensure consistency
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            // Update card status to expired (1)
                            card.CardStatus = CardStatus.Expired;
                            
                            // Create blacklist entry
                            await CreateCardBlacklistAsync(card, (int)CardStatus.Expired);
                            
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            
                            _logger.LogInformation("Successfully processed expired card {CardId}", card.CardID);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                await transaction.RollbackAsync();
                            }
                            catch (Exception rollbackEx)
                            {
                                _logger.LogError(rollbackEx, "Failed to rollback transaction for card {CardId}", card.CardID);
                            }
                            _logger.LogError(ex, "Failed to process card {CardId}: {ErrorMessage}", card.CardID, ex.Message);
                            throw new InvalidOperationException($"Failed to process card {card.CardID}", ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing card {CardId}: {Message}", card.CardID, ex.Message);
                        // Continue with next card even if one fails
                    }
                }
            }
            catch (Exception ex)
            {
                const string errorMessage = "An error occurred while checking for expired cards";
                _logger.LogError(ex, "{ErrorMessage}: {ExceptionMessage}", errorMessage, ex.Message);
                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        private async Task CreateCardBlacklistAsync(UserCard card, int status)
        {
            var existingBlacklist = await _context.CardBlacklist
                .FirstOrDefaultAsync(cb => cb.OriginalCardID == card.CardID);
                
            if (existingBlacklist != null)
            {
                _logger.LogInformation("Card {CardId} is already blacklisted", card.CardID);
                var IsBlacklisted = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardID == card.CardID);

                    // If card is blacklisted but not marked as blacklisted, mark it as blacklisted in UserCard table
                    if (!IsBlacklisted.IsBlacklisted) {
                        IsBlacklisted.IsBlacklisted = true;
                        await _context.SaveChangesAsync();
                    }

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
            
            // Create blacklist entry with UTC time for PostgreSQL compatibility
            var blacklist = new CardBlacklist
            {
                CustomerID = card.CustomerID,
                OriginalCardID = card.CardID,
                CardNumber = card.CardNumber,
                CardType = card.CardType,
                LeftOverBalance = card.Balance,
                // Use UTC time for PostgreSQL timestamp with time zone
                CardExpirationDate = DateTime.SpecifyKind(card.CardExpirationDate.Date, DateTimeKind.Utc),
                OriginalCreatedAt = DateTime.SpecifyKind(card.CreatedAt, DateTimeKind.Utc),
                Reason = (CardBlacklistReason)status,
                // BlacklistedAt = DateTime.UtcNow, // Set by DB
                Notes = $"Automatically blacklisted: {reason}"
            };
            
            // Save the blacklist entry
            await _context.CardBlacklist.AddAsync(blacklist);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created blacklist entry for card {CardId} with reason: {Reason}", card.CardID, reason);
        }
    }
}
