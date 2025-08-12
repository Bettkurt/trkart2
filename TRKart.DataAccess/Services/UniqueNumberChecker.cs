using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TRKart.Core.Interfaces;
using TRKart.DataAccess;

namespace TRKart.DataAccess.Services
{
    public class UniqueNumberChecker : IUniqueNumberChecker
    {
        private readonly ApplicationDbContext _context;

        public UniqueNumberChecker(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool> IsCardNumberUniqueAsync(string cardNumber)
        {
            return !await _context.UserCard
                .AnyAsync(uc => uc.CardNumber == cardNumber);
        }

        public async Task<bool> IsCustomerNumberUniqueAsync(string customerNumber)
        {
            return !await _context.Customers
                .AnyAsync(c => c.CustomerNumber == customerNumber);
        }
    }
}
