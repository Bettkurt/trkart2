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
        Task<bool> UpdateTransactionAsync(Transaction transaction);
        Task<bool> DeleteTransactionAsync(int CardID);
    }
} 