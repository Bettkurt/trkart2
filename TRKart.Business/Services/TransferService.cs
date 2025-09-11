using System;
using System.Threading.Tasks;
using TRKart.Business.Interfaces;
using TRKart.Business.Services;
using TRKart.DataAccess;
using Microsoft.Extensions.Logging;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;
using TRKart.Entities.Enums;
using TRKart.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace TRKart.Business.Services
{
    public class TransferService : ITransferService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInputValidationService _inputValidationService;
        private readonly ILogger<TransferService> _logger;

        public TransferService(ApplicationDbContext context,
                             IInputValidationService inputValidationService,
                             ILogger<TransferService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _inputValidationService = inputValidationService ?? throw new ArgumentNullException(nameof(inputValidationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private async Task<(object Source, decimal SourceBalance, string SourceIdentifier, bool Success, string ErrorMessage)> GetSourceDetailsAsync(TransferCreateDto dto)
        {
            if (dto.SourceType == TransferSourceType.Card)
            {
                var card = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardID == dto.SourceId);
                
                if (card == null)
                {
                    return (null, 0, null, false, "Source card not found");
                }
                
                return (card, card.Balance, card.CardNumber, true, null);
            }
            
            // Handle wallet source
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.WalletId == dto.SourceId);
            
            if (wallet == null)
            {
                return (null, 0, null, false, "Source wallet not found");
            }
            
            return (wallet, wallet.Balance, wallet.WalletNumber, true, null);
        }

        private async Task<(object Destination, string DestinationIdentifier, bool Success, string ErrorMessage)> GetDestinationDetailsAsync(TransferCreateDto dto)
        {
            if (dto.DestinationType == TransferSourceType.Card)
            {
                var card = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardNumber == dto.DestinationIdentifier);
                
                if (card == null)
                {
                    return (null, null, false, "Destination card not found");
                }
                
                return (card, card.CardNumber, true, null);
            }
            
            // Handle wallet destination
            if (!int.TryParse(dto.DestinationIdentifier, out int walletId))
            {
                return (null, null, false, "Invalid wallet identifier");
            }
            
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.WalletId == walletId);
            
            if (wallet == null)
            {
                return (null, null, false, "Destination wallet not found");
            }
            
            return (wallet, wallet.WalletNumber, true, null);
        }

        public async Task<TransferResponse> CreateTransferAsync(TransferCreateDto dto)
        {
            var response = new TransferResponse();

            try
            {
                Console.WriteLine($"TransferService: Starting transfer - SourceType={dto.SourceType}, SourceId={dto.SourceId}, DestinationType={dto.DestinationType}, Destination={dto.DestinationIdentifier}, Amount={dto.Amount}");
                
                var inputValidation = _inputValidationService.ValidateTransferInput(dto);
                if (!inputValidation.IsValid)
                {
                    response.Success = false;
                    response.Message = $"Input validation failed: {string.Join("; ", inputValidation.Errors.Select(e => $"{e.Field}: {e.Error}"))}";
                    return response;
                }

                // Get source details
                var (source, sourceBalance, sourceIdentifier, sourceSuccess, sourceError) = await GetSourceDetailsAsync(dto);
                if (!sourceSuccess)
                {
                    response.Success = false;
                    response.Message = sourceError;
                    return response;
                }

                // Get destination details
                var (destination, destinationIdentifier, destSuccess, destError) = await GetDestinationDetailsAsync(dto);
                if (!destSuccess)
                {
                    response.Success = false;
                    response.Message = destError;
                    return response;
                }

                // Validate business rules
                var businessValidation = _inputValidationService.ValidateTransferBusinessRules(dto, source, destination, sourceBalance);
                if (!businessValidation.IsValid)
                {
                    response.Success = false;
                    response.Message = $"Business validation failed: {string.Join("; ", businessValidation.Errors.Select(e => $"{e.Field}: {e.Error}"))}";
                    return response;
                }

                Console.WriteLine($"TransferService: Business validation passed - Sender balance: {sourceBalance}, Recipient status: {(destination as UserCard)?.CardStatus}");

                // Create transaction DTOs
                Console.WriteLine($"TransferService: Creating transaction DTOs");
                
                // For TransferOut (source)
                var transferOutDto = new TransactionCreateDto
                {
                    Amount = dto.Amount,
                    Description = $"Transfer to {dto.DestinationType} {destinationIdentifier}",
                    ReferenceId = $"TRF-OUT-{Guid.NewGuid()}"
                };

                // For TransferIn (destination)
                var transferInDto = new TransactionCreateDto
                {
                    Amount = dto.Amount,
                    Description = $"Transfer from {dto.SourceType} {sourceIdentifier}",
                    ReferenceId = $"TRF-IN-{Guid.NewGuid()}"
                };

                // Set source properties based on source type
                SetSourceTransactionProperties(dto, source, transferOutDto);
                
                // Set destination properties based on destination type
                SetDestinationTransactionProperties(dto, destination, transferInDto);

                // Validate TransferOut transaction feasibility
                var transferOutFeasibility = _inputValidationService.ValidateTransactionFeasibility(transferOutDto, sourceBalance);
                if (!transferOutFeasibility.IsValid)
                {
                    response.Success = false;
                    response.Message = $"TransferOut validation failed: {string.Join("; ", transferOutFeasibility.Errors.Select(e => $"{e.Field}: {e.Error}"))}";
                    return response;
                }

                // PHASE 1: Create TransferOut transaction (for sender)
                Console.WriteLine("TransferService: PHASE 1 - Creating TransferOut transaction");
                var transferOutResult = await CreateAndSaveTransactionAsync(source, transferOutDto);
                if (transferOutResult == null)
                {
                    response.Success = false;
                    response.Message = "Failed to create transfer out transaction";
                    return response;
                }

                // PHASE 2: Create TransferIn transaction (for recipient)
                Console.WriteLine("TransferService: PHASE 2 - Creating TransferIn transaction");
                var transferInResult = await CreateAndSaveTransactionAsync(destination, transferInDto);
                if (transferInResult == null)
                {
                    // If we fail to create the transfer in, we need to handle the partial transaction
                    await HandleTransferFailureAsync(transferOutResult, "Failed to create transfer in transaction", destinationIdentifier);
                    response.Success = false;
                    response.Message = "Failed to create transfer in transaction";
                    return response;
                }
                
                // Update source and destination balances
                await UpdateBalancesAsync(dto, source, destination);

                await _context.SaveChangesAsync();

                // PHASE 3: Update transaction statuses
                Console.WriteLine($"TransferService: PHASE 3 - Updating transaction statuses");
                transferOutResult.TransactionStatus = TransactionStatus.Approved.ToString();
                transferInResult.TransactionStatus = TransactionStatus.Approved.ToString();
                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Transfer completed successfully";
                response.TransferOutTransaction = transferOutResult;
                response.TransferInTransaction = transferInResult;
                
                // Only check card status if destination is a card
                if (dto.DestinationType == TransferSourceType.Card && 
                    destination is UserCard recipientCard && 
                    recipientCard.CardStatus != CardStatus.Active)
                {
                    var failureReason = $"Recipient card status changed to {recipientCard.CardStatus} during transfer. Only active cards can receive transfers.";
                    return await HandleTransferFailureAsync(transferOutResult, failureReason, recipientCard.CardNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in transfer process");
                
                response.Success = false;
                response.Message = "An unexpected error occurred while processing your transfer";
                response.Error = ex.Message;
            }

            return response;
        }

        private static void SetSourceTransactionProperties(TransferCreateDto dto, object source, TransactionCreateDto transferOutDto)
        {
            if (dto.SourceType == TransferSourceType.Card)
            {
                var sourceCard = (UserCard)source;
                transferOutDto.CardID = sourceCard.CardID;
                transferOutDto.TransactionType = TransactionType.TransferOut;
            }
            else // Source is Wallet
            {
                var sourceWallet = (Wallet)source;
                transferOutDto.WalletId = sourceWallet.WalletId;
                transferOutDto.TransactionType = TransactionType.WalletPay;
            }
        }

        private static void SetDestinationTransactionProperties(TransferCreateDto dto, object destination, TransactionCreateDto transferInDto)
        {
            if (dto.DestinationType == TransferSourceType.Card)
            {
                var destCard = (UserCard)destination;
                transferInDto.CardID = destCard.CardID;
                transferInDto.TransactionType = TransactionType.TransferIn;
            }
            else // Destination is Wallet
            {
                var destWallet = (Wallet)destination;
                transferInDto.WalletId = destWallet.WalletId;
                transferInDto.TransactionType = TransactionType.WalletLoad;
            }
        }


        private async Task<Transaction> CreateAndSaveTransactionAsync(object source, TransactionCreateDto transactionDto)
        {
            try
            {
                var transaction = new Transaction
                {
                    Amount = transactionDto.Amount,
                    Description = transactionDto.Description,
                    TransactionType = transactionDto.TransactionType,
                    Status = TransactionStatus.Approved,
                    TransactionDate = DateTime.UtcNow
                };

                if (source is UserCard card)
                {
                    transaction.CardID = card.CardID;
                }
                else if (source is Wallet wallet)
                {
                    transaction.WalletId = wallet.WalletId;
                }

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction");
                return null;
            }
        }

        private async Task UpdateBalancesAsync(TransferCreateDto dto, object source, object destination)
        {
            var amount = dto.Amount;

            // Update source balance
            if (source is UserCard sourceCard)
            {
                sourceCard.Balance -= amount;
                _context.UserCards.Update(sourceCard);
            }
            else if (source is Wallet sourceWallet)
            {
                sourceWallet.Balance -= amount;
                _context.Wallets.Update(sourceWallet);
            }

            // Update destination balance
            if (destination is UserCard destCard)
            {
                destCard.Balance += amount;
                _context.UserCards.Update(destCard);
            }
            else if (destination is Wallet destWallet)
            {
                destWallet.Balance += amount;
                _context.Wallets.Update(destWallet);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<TransferValidationResponse> ValidateRecipientCardAsync(string cardNumber)
        {
            var response = new TransferValidationResponse();

            try
            {
                var card = await _context.UserCard
                    .Where(c => c.CardNumber == cardNumber)
                    .Select(c => new UserCardDto
                    {
                        CardID = c.CardID,
                        CardNumber = c.CardNumber,
                        Balance = c.Balance,
                        CardStatus = c.CardStatus,
                        CustomerID = c.CustomerID
                    })
                    .FirstOrDefaultAsync();

                if (card == null)
                {
                    response.Success = true;
                    response.IsValid = false;
                    response.Message = "Card not found";
                }
                else if (card.CardStatus != CardStatus.Active) // Active = 4
                {
                    response.Success = true;
                    response.IsValid = false;
                    response.Message = "Transfer can not be completed.Card not found.";
                }
                else
                {
                    response.Success = true;
                    response.IsValid = true;
                    response.Message = "Card is valid for transfer";
                    response.RecipientCard = card;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.IsValid = false;
                response.Message = $"Validation failed: {ex.Message}";
            }

            return response;
        }

        public async Task<TransferResponse> GetTransferDetailsAsync(int transferTransactionID)
        {
            var response = new TransferResponse();

            try
            {
                var transaction = await _context.Transaction
                    .Include(t => t.TransferTransaction)
                    .FirstOrDefaultAsync(t => t.TransactionID == transferTransactionID);

                if (transaction == null)
                {
                    response.Success = false;
                    response.Message = "Transfer transaction not found";
                    return response;
                }

                if (transaction.TransactionType == TransactionType.TransferOut)
                {
                    response.TransferOutTransaction = transaction;
                    response.TransferInTransaction = transaction.TransferTransaction;
                }
                else if (transaction.TransactionType == TransactionType.TransferIn)
                {
                    response.TransferInTransaction = transaction;
                    response.TransferOutTransaction = transaction.TransferTransaction;
                }

                response.Success = true;
                response.Message = "Transfer details retrieved successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Failed to get transfer details: {ex.Message}";
                response.Error = ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Handles transfer failure by updating the transaction status
        /// </summary>
        private async Task<TransferResponse> HandleTransferFailureAsync(
            Transaction transferOutTransaction,
            string failureReason,
            string destinationIdentifier)
        {
            Console.WriteLine($"TransferService: Handling transfer failure: {failureReason}");
            
            // Update the transfer out transaction status to failed
            transferOutTransaction.Status = TransactionStatus.Denied;
            transferOutTransaction.Description = $"Transfer to {destinationIdentifier} failed: {failureReason}";
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating failed transaction status: {ex.Message}");
            }

            return new TransferResponse
            {
                Success = false,
                Message = $"Transfer failed: {failureReason}",
                TransferOutTransaction = transferOutTransaction,
                TransferInTransaction = null
            };
        }
    }
}