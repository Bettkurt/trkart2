using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TRKart.Entities.Models;
using TRKart.Repository.Interfaces;
using TRKart.DataAccess;

namespace TRKart.Repository.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public TransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction> AddTransactionAsync(Transaction transaction)
        {
            _context.Transaction.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByCardIdAsync(int cardId)
        {
            return await _context.Transaction.Where(t => t.CardID == cardId).ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByCustomerIdAsync(int customerId)
        {
            return await _context.Transaction
                .Include(t => t.UserCard)
                .Where(t => t.UserCard.CustomerID == customerId)
                .ToListAsync();
        }
    }
} 