using System.Collections.Generic;
using System.Threading.Tasks;
using TRKart.Business.Interfaces;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;
using TRKart.Repository.Interfaces;
using TRKart.Repository.Repositories;
using TRKart.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace TRKart.Business.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ApplicationDbContext _context;
        private readonly IInputValidationService _inputValidationService;

        public TransactionService(ITransactionRepository transactionRepository, ApplicationDbContext context, IInputValidationService inputValidationService)
        {
            _transactionRepository = transactionRepository;
            _context = context;
            _inputValidationService = inputValidationService;
        }

        public async Task<Transaction> AddTransactionAsync(TransactionCreateDto dto)
        {
            // First, validate input format and characters
            var inputValidation = _inputValidationService.ValidateTransactionInput(dto);
            if (!inputValidation.IsValid)
            {
                throw new InvalidOperationException($"Input validation failed: {string.Join("; ", inputValidation.Errors.Select(e => $"{e.Field}: {e.Error}"))}");
            }

            // Pre-check transaction feasibility before attempting database operation
            var feasibilityCheck = await CheckTransactionFeasibilityAsync(dto);
            if (!feasibilityCheck.IsFeasible)
            {
                throw new InvalidOperationException($"Transaction denied: {feasibilityCheck.Message}");
            }

            var transaction = new Transaction
            {
                CardID = dto.CardID,
                Amount = dto.Amount,
                TransactionType = dto.TransactionType,
                Description = dto.Description
            };
            return await _transactionRepository.AddTransactionAsync(transaction);
        }

        public async Task<TransactionFeasibilityResponse> CheckTransactionFeasibilityAsync(TransactionCreateDto dto)
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

        public async Task<decimal> GetCardBalanceAsync(int cardId)
        {
            return await _transactionRepository.GetCardBalanceAsync(cardId);
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByCardIdAsync(int CardID)
        {
            return await _transactionRepository.GetTransactionsByCardIdAsync(CardID);
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByCustomerIdAsync(int customerId)
        {
            return await _transactionRepository.GetTransactionsByCustomerIdAsync(customerId);
        }
    }
} 