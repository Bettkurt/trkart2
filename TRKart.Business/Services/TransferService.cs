using System;
using System.Threading.Tasks;
using TRKart.Business.Interfaces;
using TRKart.Business.Services;
using TRKart.DataAccess;
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
        private readonly ITransactionRepository _transactionRepository;
        private readonly ApplicationDbContext _context;
        private readonly IInputValidationService _inputValidationService;

        public TransferService(ITransactionRepository transactionRepository, 
                               ApplicationDbContext context,
                               IInputValidationService inputValidationService)
        {
            _transactionRepository = transactionRepository;
            _context = context;
            _inputValidationService = inputValidationService;
        }

        public async Task<TransferResponse> CreateTransferAsync(TransferCreateDto dto)
        {
            var response = new TransferResponse();

            try
            {
                Console.WriteLine($"TransferService: Starting transfer for senderCardID={dto.SenderCardID}, recipientCardNumber={dto.RecipientCardNumber}, amount={dto.Amount}");
                
                // Validate input format using InputValidationService
                var inputValidation = _inputValidationService.ValidateTransferInput(dto);
                if (!inputValidation.IsValid)
                {
                    response.Success = false;
                    response.Message = $"Input validation failed: {string.Join("; ", inputValidation.Errors.Select(e => $"{e.Field}: {e.Error}"))}";
                    return response;
                }

                // Get sender card and validate
                Console.WriteLine($"TransferService: Looking up sender card ID {dto.SenderCardID}");
                var senderCard = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardID == dto.SenderCardID);

                // Get recipient card by card number
                Console.WriteLine($"TransferService: Looking up recipient card number {dto.RecipientCardNumber}");
                var recipientCard = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardNumber == dto.RecipientCardNumber);

                // Validate business rules using InputValidationService
                var businessValidation = _inputValidationService.ValidateTransferBusinessRules(dto, senderCard, recipientCard);
                if (!businessValidation.IsValid)
                {
                    response.Success = false;
                    response.Message = $"Business validation failed: {string.Join("; ", businessValidation.Errors.Select(e => $"{e.Field}: {e.Error}"))}";
                    return response;
                }

                Console.WriteLine($"TransferService: Business validation passed - Sender balance: {senderCard.Balance}, Recipient status: {recipientCard.CardStatus}");

                // Validate both transactions before creating any
                Console.WriteLine($"TransferService: Validating both transactions before creation");
                var transferOutDto = new TransactionCreateDto
                {
                    CardID = senderCard.CardID,
                    Amount = dto.Amount,
                    TransactionType = "transferout",
                    Description = $"Transfer to card {recipientCard.CardNumber}"
                };

                var transferInDto = new TransactionCreateDto
                {
                    CardID = recipientCard.CardID,
                    Amount = dto.Amount,
                    TransactionType = "transferin",
                    Description = $"Transfer from card {senderCard.CardNumber}"
                };

                // Validate TransferOut transaction feasibility using InputValidationService
                var transferOutFeasibility = _inputValidationService.ValidateTransactionFeasibility(transferOutDto, senderCard.Balance);
                if (!transferOutFeasibility.IsValid)
                {
                    response.Success = false;
                    response.Message = $"TransferOut validation failed: {string.Join("; ", transferOutFeasibility.Errors.Select(e => $"{e.Field}: {e.Error}"))}";
                    return response;
                }

                // Validate TransferIn transaction feasibility using InputValidationService
                var transferInFeasibility = _inputValidationService.ValidateTransactionFeasibility(transferInDto, recipientCard.Balance);
                if (!transferInFeasibility.IsValid)
                {
                    response.Success = false;
                    response.Message = $"TransferIn validation failed: {string.Join("; ", transferInFeasibility.Errors.Select(e => $"{e.Field}: {e.Error}"))}";
                    return response;
                }

                // PHASE 1: Create TransferOut transaction (for sender)
                Console.WriteLine($"TransferService: PHASE 1 - Creating TransferOut transaction");
                var transferOutTransaction = new Transaction
                {
                    CardID = senderCard.CardID,
                    Amount = dto.Amount,
                    TransactionType = "TransferOut",
                    Description = $"Transfer to card {recipientCard.CardNumber}"
                };

                var transferOutResult = await _transactionRepository.AddTransactionAsync(transferOutTransaction);
                Console.WriteLine($"TransferService: TransferOut transaction created with ID {transferOutResult.TransactionID}");

                // PHASE 2: Create TransferIn transaction (for recipient)
                Console.WriteLine($"TransferService: PHASE 2 - Creating TransferIn transaction");
                
                // Double-check recipient card status before creating TransferIn
                var currentRecipientCard = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardID == recipientCard.CardID);
                
                if (currentRecipientCard == null)
                {
                    var failureReason = "Recipient card no longer exists";
                    return await HandleTransferFailureAsync(transferOutResult, failureReason, recipientCard.CardNumber);
                }
                
                if (currentRecipientCard.CardStatus != CardStatus.Active)
                {
                    var failureReason = $"Recipient card status changed to {currentRecipientCard.CardStatus} during transfer. Only active cards can receive transfers.";
                    return await HandleTransferFailureAsync(transferOutResult, failureReason, recipientCard.CardNumber);
                }
                
                var transferInTransaction = new Transaction
                {
                    CardID = recipientCard.CardID,
                    Amount = dto.Amount,
                    TransactionType = "TransferIn",
                    Description = $"Transfer from card {senderCard.CardNumber}",
                    TransferTransactionID = transferOutResult.TransactionID // Link to TransferOut
                };

                try
                {
                    var transferInResult = await _transactionRepository.AddTransactionAsync(transferInTransaction);
                    Console.WriteLine($"TransferService: TransferIn transaction created with ID {transferInResult.TransactionID}");

                    // PHASE 3: Update TransferOut transaction to link back to TransferIn
                    Console.WriteLine($"TransferService: PHASE 3 - Updating TransferOut transaction to link back");
                    transferOutResult.TransferTransactionID = transferInResult.TransactionID;
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"TransferService: Successfully updated TransferOut transaction");

                    response.Success = true;
                    response.Message = "Transfer completed successfully";
                    response.TransferInTransaction = transferInResult;
                    response.TransferOutTransaction = transferOutResult;
                    
                    Console.WriteLine($"TransferService: Transfer completed successfully");
                }
                catch (Exception transferInException)
                {
                    Console.WriteLine($"TransferService: TransferIn failed: {transferInException.Message}");
                    
                    // TransferOut succeeded but TransferIn failed - create refund
                    var failureReason = $"TransferIn creation failed: {transferInException.Message}";
                    return await HandleTransferFailureAsync(transferOutResult, failureReason, recipientCard.CardNumber);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TransferService: Exception occurred: {ex.Message}");
                Console.WriteLine($"TransferService: Stack trace: {ex.StackTrace}");
                
                response.Success = false;
                response.Message = $"Transfer failed: {ex.Message}";
                response.Error = ex.Message;
            }

            return response;
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
                        CardStatus = (CardStatus)c.CardStatus,
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

                if (transaction.TransactionType == "TransferOut")
                {
                    response.TransferOutTransaction = transaction;
                    response.TransferInTransaction = transaction.TransferTransaction;
                }
                else if (transaction.TransactionType == "TransferIn")
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
        /// Creates a refund transaction to reverse a failed transfer
        /// This method is used when TransferOut succeeds but TransferIn fails
        /// </summary>
        private async Task<Transaction> CreateRefundTransactionAsync(int cardId, decimal amount, string description)
        {
            Console.WriteLine($"TransferService: Creating refund transaction for card {cardId}, amount {amount}");
            
            var refundTransaction = new Transaction
            {
                CardID = cardId,
                Amount = amount, // Positive amount for refund
                TransactionType = "Refund",
                Description = description
            };

            var result = await _transactionRepository.AddTransactionAsync(refundTransaction);
            Console.WriteLine($"TransferService: Refund transaction created with ID {result.TransactionID}");
            
            return result;
        }

        /// <summary>
        /// Handles transfer failure by creating a refund transaction
        /// This method is called when TransferOut succeeds but TransferIn fails
        /// </summary>
        private async Task<TransferResponse> HandleTransferFailureAsync(
            Transaction transferOutTransaction, 
            string failureReason, 
            string recipientCardNumber)
        {
            Console.WriteLine($"TransferService: Handling transfer failure: {failureReason}");
            
            try
            {
                // Create refund transaction to reverse the TransferOut
                var refundDescription = $"Refund for failed transfer to card {recipientCardNumber}. Reason: {failureReason}";
                var refundTransaction = await CreateRefundTransactionAsync(
                    transferOutTransaction.CardID, 
                    transferOutTransaction.Amount, 
                    refundDescription);

                // Update TransferOut transaction to mark it as failed
                transferOutTransaction.TransactionStatus = "Denied";
                transferOutTransaction.Description = $"FAILED: {transferOutTransaction.Description}. {failureReason}";
                
                await _context.SaveChangesAsync();
                Console.WriteLine($"TransferService: TransferOut transaction marked as failed and refund created");

                return new TransferResponse
                {
                    Success = false,
                    Message = $"Transfer failed: {failureReason}. The amount has been automatically refunded to your account.",
                    TransferOutTransaction = transferOutTransaction,
                    Error = failureReason
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TransferService: Error creating refund transaction: {ex.Message}");
                throw new InvalidOperationException($"Transfer failed and refund creation failed: {ex.Message}");
            }
        }
    }
} 