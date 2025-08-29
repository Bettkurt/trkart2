using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.Models;
using TRKart.Entities.Enums;

namespace TRKart.Business.Services
{
    public class CardBalanceTransferService : ICardBalanceTransferService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CardBalanceTransferService> _logger;

        public CardBalanceTransferService(
            ApplicationDbContext context,
            ILogger<CardBalanceTransferService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> TransferBalancesFromBlacklistedCardsAsync()
        {
            int transfersCompleted = 0;
            _logger.LogInformation("Starting balance transfer from blacklisted cards at {UtcNow}", DateTime.UtcNow);

            try
            {
                // Get all blacklisted cards with positive balance
                var blacklistedCards = await _context.CardBlacklist
                    .Where(cb => cb.LeftOverBalance > 0
                    && cb.BlacklistedAt < DateTime.UtcNow.AddDays(-5)) // Cards blacklisted for at least 5 days
                    .ToListAsync();

                if (!blacklistedCards.Any())
                {
                    _logger.LogInformation("No blacklisted cards with positive balance found");
                    return 0;
                }

                _logger.LogInformation("Found {Count} blacklisted cards with positive balance", blacklistedCards.Count);

                foreach (var blacklistedCard in blacklistedCards)
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Find an active card for the same customer
                        var activeCard = await _context.UserCard
                            .Where(c => c.CustomerID == blacklistedCard.CustomerID
                                    && c.CardID != blacklistedCard.OriginalCardID
                                    && c.CardStatus == CardStatus.Active
                                    && !c.IsBlacklisted)
                            .OrderBy(c => c.CardID) // Just to be deterministic
                            .FirstOrDefaultAsync();

                        if (activeCard == null)
                        {
                            _logger.LogInformation("No active card found for customer {CustomerId} to transfer balance to", 
                                blacklistedCard.CustomerID);
                            continue;
                        }

                        // Create timestamp for consistent transaction time
                        //var transactionTime = DateTime.UtcNow;
                        
                        // Create SystemTransferOut transaction (from blacklisted card)
                        var transferOut = new Transaction
                        {
                            CardID = blacklistedCard.OriginalCardID,
                            Amount = blacklistedCard.LeftOverBalance,
                            TransactionType = "SystemTransferOut",
                            Description = $"System transfer to card {activeCard.CardNumber}",
                            // TransactionStatus = "Pending"
                        };

                        // Create SystemTransferIn transaction (to active card)
                        var transferIn = new Transaction
                        {
                            CardID = activeCard.CardID,
                            Amount = blacklistedCard.LeftOverBalance,
                            TransactionType = "SystemTransferIn",
                            Description = $"System transfer from blacklisted card {blacklistedCard.CardNumber}",
                            // TransactionStatus = "Pending"
                        };

                        // Add both transactions to the context first (without the foreign key references)
                        await _context.Transaction.AddRangeAsync(transferOut, transferIn);
                        await _context.SaveChangesAsync();

                        // Now that both transactions have IDs, we can set the TransferTransactionID
                        transferOut.TransferTransactionID = transferIn.TransactionID;
                        transferIn.TransferTransactionID = transferOut.TransactionID;

                        // Save changes to update the transaction references
                        await _context.SaveChangesAsync();

                        if (transferOut.TransactionStatus == "Approved")
                        {

                            // Update balances
                            activeCard.Balance += blacklistedCard.LeftOverBalance;
                            blacklistedCard.LeftOverBalance = 0;
                            blacklistedCard.Notes += $"| Balance transferred to card {activeCard.CardNumber} on {DateTime.UtcNow:yyyy-MM-dd}. | ";

                            // Save all changes
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            transfersCompleted++;
                            _logger.LogInformation("Transferred {Amount} from blacklisted card {SourceCardId} to card {TargetCardId}", 
                                blacklistedCard.LeftOverBalance, blacklistedCard.OriginalCardID, activeCard.CardID);
                        } else {
                            _logger.LogInformation("Transfer of {Amount} TRY from blacklisted card {SourceCardId} to card {TargetCardId} failed", 
                                blacklistedCard.LeftOverBalance, blacklistedCard.OriginalCardID, activeCard.CardID);
                        }
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error transferring balance from blacklisted card {CardId}", 
                            blacklistedCard.OriginalCardID);
                    }
                }

                _logger.LogInformation("Completed balance transfers. Total successful transfers: {Count}", transfersCompleted);
                return transfersCompleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TransferBalancesFromBlacklistedCardsAsync");
                throw;
            }
        }
    }
}