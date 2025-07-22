using Microsoft.EntityFrameworkCore;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities;
using TRKart.Entities.DTOs;

namespace TRKart.Business.Services
{
    public class UserCardService : IUserCardService
    {
        private readonly ApplicationDbContext _context;

        public UserCardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserCardDto>> GetUserCardsAsync(int customerId)
        {
            var cards = await _context.UserCards
                .Where(x => x.CustomerId == customerId)
                .Select(x => new UserCardDto
                {
                    CardId = x.CardId,
                    CustomerId = x.CustomerId,
                    Balance = x.Balance,
                    CardNumber = x.CardNumber
                })
                .ToListAsync();

            return cards;
        }

        public async Task<bool> CreateUserCardAsync(UserCardDto dto)
        {
            var newCard = new UserCard
            {
                CustomerId = dto.CustomerId,
                Balance = dto.Balance,
                CardNumber = GenerateCardNumber()
            };

            await _context.UserCards.AddAsync(newCard);
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateCardNumber()
        {
            // Use database function or generate here
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}