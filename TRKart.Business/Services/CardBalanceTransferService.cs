using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TRKart.Entities.Enums;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.DTOs;
using TRKart.Entities.Models;
using TRKart.Entities.Enums;

namespace TRKart.Business.Services
{
    public class CardBalanceTransferService : ICardBalanceTransferService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CardBalanceTransferService> _logger;
        private readonly IWalletService _walletService;

        public CardBalanceTransferService(
            ApplicationDbContext context,
            ILogger<CardBalanceTransferService> logger,
            IWalletService walletService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _walletService = walletService ?? throw new ArgumentNullException(nameof(walletService));
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
                    _logger.LogInformation("No blacklisted cards with positive balance found that meet the 5-day criteria");
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
                            _logger.LogInformation("No active card found for customer {CustomerId}, attempting to transfer to wallet", 
                                blacklistedCard.CustomerID);
                            
                            // Try to transfer to wallet instead
                            await TransferBalanceToWalletAsync(blacklistedCard);
                            transfersCompleted++;
                            await transaction.CommitAsync();
                            continue;
                        }

                        // Create SystemTransferOut transaction (from blacklisted card)
                        var transferOut = new Transaction
                        {
                            CardID = blacklistedCard.OriginalCardID,
                            Amount = blacklistedCard.LeftOverBalance,
                            TransactionType = TransactionType.SystemTransferOut,
                            Description = $"System transfer to card {activeCard.CardNumber}",
                            // TransactionStatus = "Pending"
                        };

                        // Create SystemTransferIn transaction (to active card)
                        var transferIn = new Transaction
                        {
                            CardID = activeCard.CardID,
                            Amount = blacklistedCard.LeftOverBalance,
                            TransactionType = TransactionType.SystemTransferIn,
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
                _logger.LogError(ex, "Error processing balance transfers for blacklisted cards");
                throw new InvalidOperationException("An error occurred while processing balance transfers. See logs for details.", ex);
            }
        }

        private async Task TransferBalanceToWalletAsync(CardBlacklist blacklistedCard)
        {
            // Check if we're already in a transaction
            var transaction = _context.Database.CurrentTransaction != null 
                ? null 
                : await _context.Database.BeginTransactionAsync();
                
            try
            {
                // Get or create wallet ID
                var walletId = await GetOrCreateWalletId(blacklistedCard.CustomerID);
                var amount = blacklistedCard.LeftOverBalance;

                // Create a wallet transaction DTO with the required CardId
                var walletTransaction = new WalletTransactionDto
                {
                    WalletId = walletId,
                    CardId = blacklistedCard.OriginalCardID, // This is required by LoadWalletAsync
                    Amount = amount,
                    Description = $"Transfer from blacklisted card {blacklistedCard.CardNumber}",
                    ReferenceId = $"BLK-{blacklistedCard.CardBlacklistID}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                };

                // Process the wallet load first, passing isSystemTransfer=true to bypass card status validation
                var walletTransactionResult = await _walletService.LoadWalletAsync(walletTransaction, isSystemTransfer: true);

                if (walletTransactionResult == null || walletTransactionResult.TransactionID == 0)
                {
                    throw new InvalidOperationException("Failed to process wallet load transaction");
                }

                // Create SystemTransferOut transaction (from blacklisted card)
                var transferOut = new Transaction
                {
                    CardID = blacklistedCard.OriginalCardID,
                    Amount = amount,
                    TransactionType = TransactionType.SystemTransferOut,
                    TransactionStatus = TransactionStatus.Approved.ToString(),
                    Description = $"System transfer to wallet for blacklisted card {blacklistedCard.CardNumber}",
                    TransactionDate = DateTime.UtcNow,
                    // Link to the wallet transaction
                    TransferTransactionID = walletTransactionResult.TransactionID
                };

                // Add the transaction to the context
                _context.Transaction.Add(transferOut);
                
                // Update the blacklisted card's leftover balance to zero
                blacklistedCard.LeftOverBalance = 0;
                blacklistedCard.Notes = $"Balance of {amount} transferred to wallet on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}. {blacklistedCard.Notes}";
                
                // Save all changes in a single transaction
                await _context.SaveChangesAsync();
                
                // Only commit if we created the transaction
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                _logger.LogInformation("Successfully transferred {Amount} from blacklisted card {CardId} to wallet {WalletId}", 
                    amount, blacklistedCard.OriginalCardID, walletId);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                _logger.LogError(ex, "Error transferring balance from blacklisted card {CardId} to wallet. Error: {ErrorMessage}", 
                    blacklistedCard.OriginalCardID, ex.Message);
                throw; // Re-throw to be handled by the caller the exception
            }
        }

        private async Task<int> GetOrCreateWalletId(int customerId)
        {
            // Check if wallet exists
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.CustomerID == customerId);

            if (wallet != null)
                return wallet.WalletId;

            // Create a new wallet if it doesn't exist
            var newWallet = new Wallet
            {
                CustomerID = customerId,
                Balance = 0,
                Status = CardStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Wallets.AddAsync(newWallet);
            await _context.SaveChangesAsync();

            return newWallet.WalletId;
        }
    }
}