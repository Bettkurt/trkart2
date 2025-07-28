using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TRKart.Entities.Models;

namespace TRKart.Repository.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction> AddTransactionAsync(Transaction transaction);
        Task<IEnumerable<Transaction>> GetTransactionsByCardIdAsync(int cardId);
        Task<IEnumerable<Transaction>> GetTransactionsByCustomerIdAsync(int customerId);
    }
} 