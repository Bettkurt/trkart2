using System.Collections.Generic;
using System.Threading.Tasks;
using TRKart.Business.Interfaces;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;
using TRKart.Repository.Interfaces;
using TRKart.Repository.Repositories;
using TRKart.DataAccess;

namespace TRKart.Business.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<Transaction> AddTransactionAsync(TransactionCreateDto dto)
        {
            var transaction = new Transaction
            {
                CardID = dto.CardID,
                Amount = dto.Amount,
                TransactionType = dto.TransactionType,
                Description = dto.Description
            };
            return await _transactionRepository.AddTransactionAsync(transaction);
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByCardIdAsync(int CardID)
        {
            return await _transactionRepository.GetTransactionsByCardIdAsync(CardID);
        }

        public async Task<bool> UpdateTransactionAsync(Transaction transaction)
        {
            return await _transactionRepository.UpdateTransactionAsync(transaction);
        }

        public async Task<bool> DeleteTransactionAsync(int CardID)
        {
            return await _transactionRepository.DeleteTransactionAsync(CardID);
        }
    }
} 