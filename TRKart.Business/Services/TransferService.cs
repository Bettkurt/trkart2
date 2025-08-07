using System;
using System.Threading.Tasks;
using TRKart.Business.Interfaces;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;
using TRKart.Repository.Interfaces;
using TRKart.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace TRKart.Business.Services
{
    public class TransferService : ITransferService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ApplicationDbContext _context;
        private readonly IInputValidationService _inputValidationService;

        public TransferService(ITransactionRepository transactionRepository, ApplicationDbContext context, IInputValidationService inputValidationService)
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

                // Create TransferIn transaction first (for recipient)
                Console.WriteLine($"TransferService: Creating TransferIn transaction");
                var transferInTransaction = new Transaction
                {
                    CardID = recipientCard.CardID,
                    Amount = dto.Amount,
                    TransactionType = "TransferIn",
                    Description = $"Transfer from card {senderCard.CardNumber}"
                };

                var transferInResult = await _transactionRepository.AddTransactionAsync(transferInTransaction);
                Console.WriteLine($"TransferService: TransferIn transaction created with ID {transferInResult.TransactionID}");

                // Create TransferOut transaction (for sender)
                Console.WriteLine($"TransferService: Creating TransferOut transaction");
                var transferOutTransaction = new Transaction
                {
                    CardID = senderCard.CardID,
                    Amount = -dto.Amount, // Negative amount for outgoing transfer
                    TransactionType = "TransferOut",
                    Description = $"Transfer to card {recipientCard.CardNumber}",
                    TransferTransactionID = transferInResult.TransactionID // Link to TransferIn
                };

                var transferOutResult = await _transactionRepository.AddTransactionAsync(transferOutTransaction);
                Console.WriteLine($"TransferService: TransferOut transaction created with ID {transferOutResult.TransactionID}");

                // Update TransferIn transaction to link back to TransferOut
                Console.WriteLine($"TransferService: Updating TransferIn transaction to link back");
                try
                {
                    transferInResult.TransferTransactionID = transferOutResult.TransactionID;
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"TransferService: Successfully updated TransferIn transaction");
                }
                catch (Exception updateEx)
                {
                    Console.WriteLine($"TransferService: Warning - Failed to update TransferIn transaction link: {updateEx.Message}");
                    // Continue anyway since the transfer was successful
                }

                response.Success = true;
                response.Message = "Transfer completed successfully";
                response.TransferInTransaction = transferInResult;
                response.TransferOutTransaction = transferOutResult;
                
                Console.WriteLine($"TransferService: Transfer completed successfully");
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
                else if (card.CardStatus != "Active")
                {
                    response.Success = true;
                    response.IsValid = false;
                    response.Message = "Card is not active";
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
    }
} 