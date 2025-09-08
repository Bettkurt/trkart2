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

        /// <summary>
        /// Creates a transfer using proper approval workflow with business rules:
        /// 1. Comprehensive validation (cards, balance, limits, risk assessment)
        /// 2. Create TransferOut transaction and wait for approval (database trigger handles approval)
        /// 3. If TransferOut is approved, create TransferIn transaction
        /// 4. If TransferIn is approved, complete transfer
        /// 5. If any step fails, create automatic refund
        /// </summary>
        public async Task<TransferResponse> CreateTransferAsync(TransferCreateDto dto)
        {
            var response = new TransferResponse();

            try
            {
                Console.WriteLine($"TransferService: Starting transfer for senderCardID={dto.SenderCardID}, recipientCardNumber={dto.RecipientCardNumber}, amount={dto.Amount}");
                
                // PHASE 1: COMPREHENSIVE VALIDATION
                Console.WriteLine($"TransferService: PHASE 1 - Comprehensive validation");
                
                // Basic input validation
                var inputValidation = _inputValidationService.ValidateTransferInput(dto);
                if (!inputValidation.IsValid)
                {
                    response.Success = false;
                    response.Message = $"Input validation failed: {string.Join("; ", inputValidation.Errors.Select(e => $"{e.Field}: {e.Error}"))}";
                    return response;
                }

                // Get sender and recipient cards
                var senderCard = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardID == dto.SenderCardID);
                var recipientCard = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardNumber == dto.RecipientCardNumber);

                var cardValidation = _inputValidationService.ValidateTransferBusinessRules(dto, senderCard, recipientCard);
                if (!cardValidation.IsValid)
                {
                    response.Success = false;
                    response.Message = $"Card validation failed: {string.Join("; ", cardValidation.Errors.Select(e => $"{e.Field}: {e.Error}"))}";
                    return response;
                }

                Console.WriteLine($"TransferService: All validations passed - Sender balance: {senderCard.Balance}, Recipient status: {recipientCard.CardStatus}");

                // Validate both transactions before creating any
                Console.WriteLine($"TransferService: Validating both transactions before creation");
                var transferOutDto = new TransactionCreateDto
                {
                    CardID = senderCard.CardID,
                    Amount = dto.Amount,
                    TransactionType = TransactionType.TransferOut,
                    Description = $"Transfer to card {recipientCard.CardNumber}"
                };

                // Validate TransferOut transaction feasibility using InputValidationService
                var transferOutFeasibility = _inputValidationService.ValidateTransactionFeasibility(transferOutDto, senderCard.Balance);
                if (!transferOutFeasibility.IsValid)
                {
                    response.Success = false;
                    response.Message = $"TransferOut validation failed: {string.Join("; ", transferOutFeasibility.Errors.Select(e => $"{e.Field}: {e.Error}"))}";
                    return response;
                }

                var transferInDto = new TransactionCreateDto
                {
                    CardID = recipientCard.CardID,
                    Amount = dto.Amount,
                    TransactionType = TransactionType.TransferIn,
                    Description = $"Transfer from card {senderCard.CardNumber}"
                };

                // Validate TransferIn transaction feasibility using InputValidationService
                var transferInFeasibility = _inputValidationService.ValidateTransactionFeasibility(transferInDto, recipientCard.Balance);
                if (!transferInFeasibility.IsValid)
                {
                    response.Success = false;
                    response.Message = $"TransferIn validation failed: {string.Join("; ", transferInFeasibility.Errors.Select(e => $"{e.Field}: {e.Error}"))}";
                    return response;
                }

                // PHASE 2: CREATE TRANSFEROUT TRANSACTION
                Console.WriteLine($"TransferService: PHASE 2 - Creating TransferOut transaction");
                
                var transferOutTransaction = new Transaction
                {
                    CardID = senderCard.CardID,
                    Amount = dto.Amount,
                    TransactionType = TransactionType.TransferOut,
                    Description = $"Transfer to card {recipientCard.CardNumber}"
                    // TransactionStatus and TransactionDate are set by database
                };

                var transferOutResult = await _transactionRepository.AddTransactionAsync(transferOutTransaction);
                Console.WriteLine($"TransferService: TransferOut transaction created with ID {transferOutResult.TransactionID}");

                // PHASE 3: CHECK TRANSFEROUT APPROVAL (handled by database trigger)
                Console.WriteLine($"TransferService: PHASE 3 - Checking TransferOut approval from database trigger");
                
                // Check TransferOut status after database trigger processing
                var updatedTransferOut = await _context.Transaction
                    .FirstOrDefaultAsync(t => t.TransactionID == transferOutResult.TransactionID);
                
                if (updatedTransferOut == null)
                {
                    return await HandleTransferFailureAsync(transferOutResult, "TransferOut transaction not found after creation", recipientCard.CardNumber);
                }

                if (updatedTransferOut.TransactionStatus == "Denied")
                {
                    Console.WriteLine($"TransferService: TransferOut denied with ID {updatedTransferOut.TransactionID}. Reason: {updatedTransferOut.Description}");
                    return new TransferResponse
                    {
                        Success = false,
                        Message = $"Transfer denied: {updatedTransferOut.Description}",
                        TransferOutTransaction = updatedTransferOut,
                        Error = updatedTransferOut.Description
                    };
                }
                
                Console.WriteLine($"TransferService: TransferOut approved with ID {updatedTransferOut.TransactionID}");

                // PHASE 4: CREATE TRANSFERIN TRANSACTION
                Console.WriteLine($"TransferService: PHASE 4 - Creating TransferIn transaction");
                
                // Re-validate recipient card status before creating TransferIn
                var currentRecipientCard = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardID == recipientCard.CardID);
                
                if (currentRecipientCard == null)
                {
                    return await HandleTransferFailureAsync(updatedTransferOut, "Recipient card no longer exists", recipientCard.CardNumber);
                }
                
                if (currentRecipientCard.CardStatus != CardStatus.Active)
                {
                    return await HandleTransferFailureAsync(updatedTransferOut, $"Recipient card status changed to {currentRecipientCard.CardStatus} during transfer", recipientCard.CardNumber);
                }
                
                var transferInTransaction = new Transaction
                {
                    CardID = recipientCard.CardID,
                    Amount = dto.Amount,
                    TransactionType = TransactionType.TransferIn,
                    Description = $"Transfer from card {senderCard.CardNumber}",
                    TransferTransactionID = updatedTransferOut.TransactionID // Link to TransferOut
                    // TransactionStatus and TransactionDate are set by database
                };

                var transferInResult = await _transactionRepository.AddTransactionAsync(transferInTransaction);
                Console.WriteLine($"TransferService: TransferIn transaction created with ID {transferInResult.TransactionID}, Status: {transferInResult.TransactionStatus}");

                // PHASE 5: CHECK TRANSFERIN APPROVAL (handled by database trigger)
                Console.WriteLine($"TransferService: PHASE 5 - Checking TransferIn approval from database trigger");
                
                // Check TransferIn status after database trigger processing
                var updatedTransferIn = await _context.Transaction
                    .FirstOrDefaultAsync(t => t.TransactionID == transferInResult.TransactionID);
                
                if (updatedTransferIn == null)
                {
                    return await HandleTransferFailureAsync(updatedTransferOut, "TransferIn transaction not found after creation", recipientCard.CardNumber);
                }

                if (updatedTransferIn.TransactionStatus == "Denied")
                {
                    Console.WriteLine($"TransferService: TransferIn denied with ID {updatedTransferIn.TransactionID}. Reason: {updatedTransferIn.Description}");
                    return await HandleTransferFailureAsync(updatedTransferOut, updatedTransferIn.Description, recipientCard.CardNumber);
                }
                
                // Update TransferOut to link back to TransferIn
                updatedTransferOut.TransferTransactionID = updatedTransferIn.TransactionID;
                
                await _context.SaveChangesAsync();
                Console.WriteLine($"TransferService: TransferIn approved with ID {updatedTransferIn.TransactionID}");

                // PHASE 6: TRANSFER COMPLETED SUCCESSFULLY
                Console.WriteLine($"TransferService: PHASE 6 - Transfer completed successfully");
                
                response.Success = true;
                response.Message = "Transfer completed successfully";
                response.TransferInTransaction = updatedTransferIn;
                response.TransferOutTransaction = updatedTransferOut;
                
                Console.WriteLine($"TransferService: Transfer completed successfully - TransferOut: {updatedTransferOut.TransactionID}, TransferIn: {updatedTransferIn.TransactionID}");
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
                    response.Message = "Transfer can not be completed. No available card found.";
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
                TransactionType = TransactionType.Refund,
                Description = description
                // TransactionStatus and TransactionDate are set by database
            };

            var result = await _transactionRepository.AddTransactionAsync(refundTransaction);
            Console.WriteLine($"TransferService: Refund transaction created with ID {result.TransactionID}");
            
            return result;
        }

        /// <summary>
        /// Handles transfer failure by creating a refund transaction
        /// This method is called when TransferOut is approved but TransferIn fails or is denied
        /// </summary>
        private async Task<TransferResponse> HandleTransferFailureAsync(
            Transaction transferOutTransaction, 
            string failureReason, 
            string recipientCardNumber)
        {
            Console.WriteLine($"TransferService: Handling transfer failure: {failureReason}");
            
            try
            {
                // Only create refund if TransferOut was actually approved (money was deducted)
                if (transferOutTransaction.TransactionStatus == "Approved")
                {
                    // Create refund transaction to reverse the TransferOut
                    var refundDescription = $"Automatic refund for failed transfer to card {recipientCardNumber}. Reason: {failureReason}";
                    var refundTransaction = await CreateRefundTransactionAsync(
                        transferOutTransaction.CardID, 
                        transferOutTransaction.Amount, 
                        refundDescription);

                    Console.WriteLine($"TransferService: Refund transaction created with ID {refundTransaction.TransactionID}");
                }

                // Update TransferOut transaction to mark it as failed
                transferOutTransaction.TransactionStatus = "Denied";
                transferOutTransaction.Description = $"FAILED: {transferOutTransaction.Description}. {failureReason}";
                
                await _context.SaveChangesAsync();
                Console.WriteLine($"TransferService: TransferOut transaction marked as failed");

                var responseMessage = transferOutTransaction.TransactionStatus == "Approved" 
                    ? $"Transfer failed: {failureReason}. The amount has been automatically refunded to your account."
                    : $"Transfer failed: {failureReason}. No funds were deducted.";

                return new TransferResponse
                {
                    Success = false,
                    Message = responseMessage,
                    TransferOutTransaction = transferOutTransaction,
                    Error = failureReason
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TransferService: Error handling transfer failure: {ex.Message}");
                throw new InvalidOperationException($"Transfer failed and error handling failed: {ex.Message}");
            }
        }
    }

} 