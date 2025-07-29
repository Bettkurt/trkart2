using System.Collections.Generic;
using System.Threading.Tasks;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;

namespace TRKart.Business.Interfaces
{
    public interface ITransactionService
    {
        Task<Transaction> AddTransactionAsync(TransactionCreateDto dto);
        Task<IEnumerable<Transaction>> GetTransactionsByCardIdAsync(int CardID);
        Task<IEnumerable<Transaction>> GetTransactionsByCustomerIdAsync(int customerId);
        Task<TransactionFeasibilityResponse> CheckTransactionFeasibilityAsync(TransactionCreateDto dto);
        Task<decimal> GetCardBalanceAsync(int cardId);
    }
} 