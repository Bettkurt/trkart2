using System;
using System.Threading.Tasks;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;
using TRKart.Entities.Enums;
using TRKart.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace TRKart.Business.Services
{
    public class TransferService : ITransferService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ApplicationDbContext _context;

        public TransferService(ITransactionRepository transactionRepository, ApplicationDbContext context)
        {
            _transactionRepository = transactionRepository;
            _context = context;
        }

        public async Task<TransferResponse> CreateTransferAsync(TransferCreateDto dto)
        {
            var response = new TransferResponse();

            try
            {
                Console.WriteLine($"TransferService: Starting transfer for senderCardID={dto.SenderCardID}, recipientCardNumber={dto.RecipientCardNumber}, amount={dto.Amount}");
                
                // Validate input
                if (dto.Amount <= 0)
                {
                    response.Success = false;
                    response.Message = "Transfer amount must be positive";
                    return response;
                }

                // Get sender card and validate
                Console.WriteLine($"TransferService: Looking up sender card ID {dto.SenderCardID}");
                var senderCard = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardID == dto.SenderCardID);

                if (senderCard == null)
                {
                    response.Success = false;
                    response.Message = "Sender card not found";
                    return response;
                }

                Console.WriteLine($"TransferService: Sender card found - Balance: {senderCard.Balance}");

                if (senderCard.Balance < dto.Amount)
                {
                    response.Success = false;
                    response.Message = "Insufficient balance for transfer";
                    return response;
                }

                // Get recipient card by card number
                Console.WriteLine($"TransferService: Looking up recipient card number {dto.RecipientCardNumber}");
                var recipientCard = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardNumber == dto.RecipientCardNumber);

                if (recipientCard == null)
                {
                    response.Success = false;
                    response.Message = "Recipient card not found";
                    return response;
                }

                Console.WriteLine($"TransferService: Recipient card found - CardID: {recipientCard.CardID}");

                if (senderCard.CardID == recipientCard.CardID)
                {
                    response.Success = false;
                    response.Message = "Cannot transfer to the same card";
                    return response;
                }

                // Validate recipient card status - must be Active for transfers
                Console.WriteLine($"TransferService: Validating recipient card status: {recipientCard.CardStatus}");
                if (recipientCard.CardStatus != "Active")
                {
                    response.Success = false;
                    response.Message = "Transfer can not be completed. Card not found.";
                    return response;
                }

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

                // Validate TransferOut transaction feasibility
                var transferOutFeasibility = await CheckTransactionFeasibilityAsync(transferOutDto);
                if (!transferOutFeasibility.IsFeasible)
                {
                    response.Success = false;
                    response.Message = $"TransferOut validation failed: {transferOutFeasibility.Message}";
                    return response;
                }

                // Validate TransferIn transaction feasibility
                var transferInFeasibility = await CheckTransactionFeasibilityAsync(transferInDto);
                if (!transferInFeasibility.IsFeasible)
                {
                    response.Success = false;
                    response.Message = $"TransferIn validation failed: {transferInFeasibility.Message}";
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
                
                if (currentRecipientCard.CardStatus != "Active")
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

        private async Task<TransactionFeasibilityResponse> CheckTransactionFeasibilityAsync(TransactionCreateDto dto)
        {
            var response = new TransactionFeasibilityResponse();

            // Check if card exists and get current balance
            var card = await _context.UserCard
                .Where(c => c.CardID == dto.CardID)
                .Select(c => new { c.Balance, c.CardStatus, c.CardNumber })
                .FirstOrDefaultAsync();

            if (card == null)
            {
                response.IsFeasible = false;
                response.Message = "Card not found";
                return response;
            }

            response.CardNumber = card.CardNumber;
            response.CurrentBalance = card.Balance;

            // Calculate projected balance based on transaction type
            decimal projectedBalance = card.Balance;
            
            switch (dto.TransactionType.ToLower())
            {
                case "transferout":
                case "pay":
                    projectedBalance -= dto.Amount;
                    break;
                case "load":
                case "transferin":
                case "refund":
                    projectedBalance += dto.Amount;
                    break;
                default:
                    response.IsFeasible = false;
                    response.Message = $"Invalid transaction type: {dto.TransactionType}";
                    return response;
            }

            response.ProjectedBalance = projectedBalance;

            // Check if transaction would result in negative balance
            if (projectedBalance < 0)
            {
                response.IsFeasible = false;
                response.Message = $"Insufficient funds. Current balance: {card.Balance:C}, Required: {dto.Amount:C}, Projected balance: {projectedBalance:C}";
                return response;
            }

            // Check for reasonable transaction amount (optional business rule)
            if (dto.Amount <= 0)
            {
                response.IsFeasible = false;
                response.Message = "Transaction amount must be greater than zero";
                return response;
            }

            // All checks passed
            response.IsFeasible = true;
            response.Message = "Transaction is feasible";
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