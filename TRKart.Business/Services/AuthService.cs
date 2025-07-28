using Microsoft.EntityFrameworkCore;
using TRKart.Business.Interfaces;
using TRKart.Core.Helpers;
using TRKart.DataAccess;
using TRKart.Entities;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;

namespace TRKart.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtHelper _jwtHelper;

        public AuthService(ApplicationDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
        }

        public async Task<(string? Token, DateTime? Expiration)> LoginAsync(LoginDto dto)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Email == dto.Email);
            if (customer == null)
                return (null, null);
            bool isValid = BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash);
            if (!isValid)
                return (null, null);
            string token = _jwtHelper.GenerateToken(customer.Email);
            DateTime expiration = dto.RememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddHours(1);
            var session = new SessionToken
            {
                CustomerID = customer.CustomerID,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                Expiration = expiration
            };
            await _context.SessionToken.AddAsync(session);
            await _context.SaveChangesAsync();
            return (token, expiration);
        }

        public async Task<bool> RegisterAsync(RegisterDto dto)
        {
            // Aynı e-posta adresine sahip kullanıcı var mı kontrol et
            var exists = await _context.Customers.AnyAsync(x => x.Email == dto.Email);
            if (exists)
                return false;

            // Yeni kullanıcı oluştur
            var newCustomer = new Customers
            {
                Email = dto.Email,
                FullName = dto.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            await _context.Customers.AddAsync(newCustomer);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}