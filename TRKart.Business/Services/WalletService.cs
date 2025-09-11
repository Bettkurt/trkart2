using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.DTOs;
using TRKart.Entities.Enums;
using TRKart.Entities.Models;

namespace TRKart.Business.Services
{
    public class WalletService : IWalletService
    {
        private const string WalletNotFoundMessage = "Wallet not found.";
        private const decimal MinimumCardBalance = 0m;
        
        private readonly ApplicationDbContext _context;
        private readonly IUserCardService _userCardService;
        private readonly ILogger<WalletService> _logger;

        public WalletService(
            ApplicationDbContext context, 
            IUserCardService userCardService,
            ILogger<WalletService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userCardService = userCardService ?? throw new ArgumentNullException(nameof(userCardService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<WalletDto> GetWalletByCustomerIdAsync(int customerId)
        {
            var wallet = await _context.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.CustomerID == customerId);

            if (wallet == null)
                return null;

            return new WalletDto
            {
                WalletId = wallet.WalletId,
                CustomerId = wallet.CustomerID,
                WalletNumber = wallet.WalletNumber,
                Balance = wallet.Balance,
                Status = wallet.Status,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt
            };
        }

        public async Task<WalletDto> CreateWalletAsync(CreateWalletDto createWalletDto)
        {
            // Check if wallet already exists for customer
            var existingWallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.CustomerID == createWalletDto.CustomerId);

            if (existingWallet != null)
            {
                throw new InvalidOperationException("A wallet already exists for this customer.");
            }

            // Generate wallet number (you might want to implement a proper wallet number generator)
            var walletNumber = "W" + Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper();

            var wallet = new Wallet
            {
                CustomerID = createWalletDto.CustomerId,
                WalletNumber = walletNumber,
                Balance = createWalletDto.InitialBalance,
                Status = CardStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            // Map the created wallet back to DTO
            return new WalletDto
            {
                WalletId = wallet.WalletId,
                CustomerId = wallet.CustomerID,
                WalletNumber = wallet.WalletNumber,
                Balance = wallet.Balance,
                Status = wallet.Status,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt
            };
        }

        public async Task<WalletDto> UpdateWalletStatusAsync(int walletId, CardStatus newStatus)
        {
            var wallet = await _context.Wallets.FindAsync(walletId);
            if (wallet == null)
            {
                throw new KeyNotFoundException(WalletNotFoundMessage);
            }

            wallet.Status = newStatus;
            wallet.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new WalletDto
            {
                WalletId = wallet.WalletId,
                CustomerId = wallet.CustomerID,
                WalletNumber = wallet.WalletNumber,
                Balance = wallet.Balance,
                Status = wallet.Status,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt
            };
        }

        private async Task<Wallet> GetAndValidateWalletAsync(int walletId)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.WalletId == walletId);

            if (wallet == null)
                throw new KeyNotFoundException($"Wallet with ID {walletId} not found.");

            if (wallet.Status != CardStatus.Active)
                throw new InvalidOperationException($"Wallet {wallet.WalletId} is not active.");

            return wallet;
        }

        private async Task<UserCard> GetAndValidateCardAsync(int cardId, decimal requiredAmount)
        {
            var card = await _context.UserCard
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CardID == cardId);

            if (card == null)
                throw new KeyNotFoundException($"Card with ID {cardId} not found.");

            if (card.CardStatus != CardStatus.Active)
                throw new InvalidOperationException($"Card {card.CardNumber} is not active.");

            if (card.Balance < requiredAmount)
            {
                throw new InvalidOperationException(
                    $"Insufficient balance on card {card.CardNumber}. " +
                    $"Current balance: {card.Balance}, Required: {requiredAmount}");
            }

            return card;
        }

        private async Task<UserCard> GetTrackedCardForUpdateAsync(int cardId)
        {
            var card = await _context.UserCard.FindAsync(cardId);
            if (card == null)
                throw new KeyNotFoundException($"Card with ID {cardId} not found during transaction processing.");
            return card;
        }

        public async Task<Transaction> LoadWalletAsync(WalletTransactionDto transactionDto, bool isSystemTransfer = false)
        {
            if (transactionDto == null)
                throw new ArgumentNullException(nameof(transactionDto));

            if (transactionDto.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(transactionDto.Amount));

            if (!transactionDto.CardId.HasValue || transactionDto.CardId <= 0)
                throw new ArgumentException("A valid card ID is required to load the wallet.", nameof(transactionDto.CardId));

            // Check if we're already in a transaction
            var shouldCommit = _context.Database.CurrentTransaction == null;
            var dbTransaction = shouldCommit 
                ? await _context.Database.BeginTransactionAsync()
                : null;
            
            try
            {
                var wallet = await GetAndValidateWalletAsync(transactionDto.WalletId);
                
                // For system transfers, we need to handle blacklisted cards differently
                UserCard card;
                if (isSystemTransfer)
                {
                    card = await _context.UserCard.FindAsync(transactionDto.CardId.Value);
                    if (card == null)
                        throw new KeyNotFoundException($"Card with ID {transactionDto.CardId.Value} not found.");
                    
                    // For system transfers, we don't modify the card's balance since it's already been moved to LeftOverBalance
                    _logger.LogInformation("System transfer: Loading wallet from blacklisted card {CardId}. No balance deduction needed.", card.CardID);
                    
                    // Get wallet and update balance
                    var systemWallet = await GetAndValidateWalletAsync(transactionDto.WalletId);
                    systemWallet.Balance += transactionDto.Amount;
                    systemWallet.UpdatedAt = DateTime.UtcNow;
                    
                    // Create wallet transaction (credit)
                    var systemExternalRef = !string.IsNullOrEmpty(transactionDto.ReferenceId) 
                        ? transactionDto.ReferenceId 
                        : $"SYS-LOAD-{DateTime.UtcNow:yyyyMMddHHmmss}";
                        
                    var systemWalletTransaction = new Transaction
                    {
                        WalletId = systemWallet.WalletId,
                        Amount = transactionDto.Amount,
                        TransactionType = TransactionType.TransferIn,
                        Description = $"System transfer from blacklisted card {card.CardNumber}",
                        ExternalRef = systemExternalRef,
                        TransactionDate = DateTime.UtcNow,
                        TransactionStatus = TransactionStatus.Approved.ToString()
                    };
                    
                    _context.Transaction.Add(systemWalletTransaction);
                    await _context.SaveChangesAsync();
                    
                    if (shouldCommit && dbTransaction != null)
                    {
                        await dbTransaction.CommitAsync();
                    }

                    _logger.LogInformation("Successfully loaded {Amount} to wallet {WalletId} from card {CardId}", 
                        transactionDto.Amount, wallet.WalletId, card.CardID);
                        
                    return systemWalletTransaction;
                }
                else
                {
                    card = await GetAndValidateCardAsync(transactionDto.CardId.Value, transactionDto.Amount);
                }
                var trackedCard = await GetTrackedCardForUpdateAsync(card.CardID);

                // Deduct amount from card and update last update time
                trackedCard.Balance -= transactionDto.Amount;
                trackedCard.LastUpdate = DateTime.UtcNow;

                // Add amount to wallet
                wallet.Balance += transactionDto.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                // Create card transaction (debit) - only set CardID for card transactions
                var externalRef = !string.IsNullOrEmpty(transactionDto.ReferenceId) 
                    ? transactionDto.ReferenceId 
                    : $"LOAD-{DateTime.UtcNow:yyyyMMddHHmmss}";

                var cardTransaction = new Transaction
                {
                    CardID = trackedCard.CardID,
                    // Don't set WalletId for card transactions
                    Amount = transactionDto.Amount,
                    TransactionType = TransactionType.TransferOut,
                    Description = $"Wallet load to wallet {wallet.WalletNumber}",
                    ExternalRef = externalRef,
                    TransactionDate = DateTime.UtcNow,
                    TransactionStatus = TransactionStatus.Approved.ToString()
                };

                // Create wallet transaction (credit) - only set WalletId for wallet transactions
                var walletTransaction = new Transaction
                {
                    WalletId = wallet.WalletId,
                    // Don't set CardID for wallet transactions
                    Amount = transactionDto.Amount,
                    TransactionType = TransactionType.TransferIn,
                    Description = $"Load from card {trackedCard.CardNumber}",
                    ExternalRef = externalRef,
                    TransactionDate = DateTime.UtcNow,
                    TransactionStatus = TransactionStatus.Approved.ToString()
                };

                    // Save changes
                    _context.Transaction.Add(cardTransaction);
                    _context.Transaction.Add(walletTransaction);
                    
                    await _context.SaveChangesAsync();
                    
                    if (shouldCommit && dbTransaction != null)
                    {
                        await dbTransaction.CommitAsync();
                    }

                    _logger.LogInformation("Successfully loaded {Amount} to wallet {WalletId} from card {CardId}", 
                        transactionDto.Amount, wallet.WalletId, card.CardID);
                        
                    return walletTransaction;
            }
            catch (Exception ex)
            {
                try
                {
                    if (shouldCommit && dbTransaction != null)
                    {
                        await dbTransaction.RollbackAsync();
                    }
                    _logger.LogError(ex, "Error loading wallet {WalletId} with card {CardId}. Error: {ErrorMessage}", 
                        transactionDto.WalletId, transactionDto.CardId, ex.Message);
                    throw; // Re-throw to be handled by the caller
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error rolling back transaction for wallet {WalletId}", 
                        transactionDto.WalletId);
                    throw new InvalidOperationException("An error occurred while rolling back the transaction.", rollbackEx);
                }
                
                // This line is unreachable due to the throw in the catch block above
                // It's safe to remove it
            }
        }

        public async Task<Transaction> PayFromWalletAsync(WalletTransactionDto transactionDto)
        {
            if (transactionDto.WalletId <= 0)
            {
                throw new ArgumentException("Valid Wallet ID is required for payment.");
            }

            // Convert amount to positive if it's negative (frontend might send positive amount for payment)
            var paymentAmount = transactionDto.Amount < 0 ? -transactionDto.Amount : transactionDto.Amount;
            
            if (paymentAmount <= 0)
            {
                throw new ArgumentException("Payment amount must be greater than zero.");
            }

            var wallet = await _context.Wallets.FindAsync(transactionDto.WalletId);
            if (wallet == null)
            {
                throw new KeyNotFoundException(WalletNotFoundMessage);
            }

            if (wallet.Status != CardStatus.Active)
            {
                throw new InvalidOperationException("Cannot process payment from an inactive wallet.");
            }

            if (wallet.Balance < transactionDto.Amount)
            {
                throw new InvalidOperationException("Insufficient balance in wallet.");
            }

            // Verify card exists if CardId is provided
            if (transactionDto.CardId.HasValue && transactionDto.CardId > 0)
            {
                var card = await _context.UserCard.FindAsync(transactionDto.CardId);
                if (card == null)
                {
                    throw new KeyNotFoundException("The specified card was not found.");
                }
            }

            // Create transaction record
            // Parse TransactionType from string to enum
            if (!Enum.TryParse<TRKart.Entities.Enums.TransactionType>(transactionDto.TransactionType, true, out var transactionType))
            {
                transactionType = TRKart.Entities.Enums.TransactionType.Pay; // Default to Pay if parsing fails
            }

            var transaction = new Transaction
            {
                WalletId = wallet.WalletId,
                CardID = transactionDto.CardId,
                // Store the amount as negative in the transaction record
                Amount = -paymentAmount,
                TransactionType = transactionType,
                Description = transactionDto.Description,
                ExternalRef = transactionDto.ReferenceId,
                TransactionDate = DateTime.UtcNow,
                TransactionStatus = TransactionStatus.Approved.ToString()
            };

            // Update wallet balance (subtract the positive payment amount)
            wallet.Balance -= paymentAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            _context.Transaction.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }

        public async Task<decimal> GetWalletBalanceAsync(int walletId)
        {
            var wallet = await _context.Wallets.FindAsync(walletId);
            if (wallet == null)
            {
                throw new KeyNotFoundException(WalletNotFoundMessage);
            }

            return wallet.Balance;
        }

        public async Task<List<WalletTransactionDto>> GetWalletTransactionsAsync(int walletId)
        {
            // Check if wallet exists
            var walletExists = await _context.Wallets.AnyAsync(w => w.WalletId == walletId);
            if (!walletExists)
            {
                throw new KeyNotFoundException(WalletNotFoundMessage);
            }

            // Get transactions for the wallet first, then map to DTO
            var transactions = await _context.Transaction
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new 
                {
                    t.TransactionID,
                    t.WalletId,
                    t.CardID,
                    t.Amount,
                    t.TransactionType,
                    t.Description,
                    t.ExternalRef,
                    t.TransactionDate,
                    t.TransactionStatus
                })
                .AsNoTracking()
                .ToListAsync();

            // Map to DTO in memory
            var result = transactions.Select(t => new WalletTransactionDto
            {
                TransactionId = t.TransactionID,
                WalletId = t.WalletId ?? 0,
                CardId = t.CardID,
                Amount = t.Amount,
                TransactionType = t.TransactionType.ToString().ToUpper(),
                Description = t.Description ?? string.Empty,
                ReferenceId = t.ExternalRef,
                TransactionDate = t.TransactionDate,
                Status = (t.TransactionStatus ?? "COMPLETED").ToUpper()
            }).ToList();
            
            return result;
        }
    }
}
