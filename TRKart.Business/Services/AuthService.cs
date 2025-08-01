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

        public async Task<(string? Token, DateTime? ExpirationDate)> LoginAsync(LoginDto dto)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Email == dto.Email);
            if (customer == null)
                return (null, null);
            bool isValid = BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash);
            if (!isValid)
                return (null, null);
            string token = _jwtHelper.GenerateToken(customer.Email);
            
            // A session has a maximum duration of 1 hour
            DateTime expiration = DateTime.UtcNow.AddHours(1); 
            var session = new SessionToken
            {
                CustomerID = customer.CustomerID,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpirationDate = expiration
            };
            await _context.SessionToken.AddAsync(session);
            await _context.SaveChangesAsync();
            return (token, expiration);
        }

        public async Task<(bool IsValid, string? Email)> ValidateSessionAsync(string token)
        {
            var session = await _context.SessionToken
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Token == token && s.ExpirationDate > DateTime.UtcNow);
            
            if (session == null)
                return (false, null);
            
            return (true, session.Customer.Email);
        }

        public async Task<bool> InvalidateSessionAsync(string token)
        {
            try {
                if (string.IsNullOrEmpty(token)) {
                    return true;
                }

                var session = await _context.SessionToken
                    .FirstOrDefaultAsync(s => s.Token == token);

                // If the session doesn't exist, it's already invalid
                if (session == null) {
                    return true;
                }
                
                // Set expiration to now to invalidate the token
                session.ExpirationDate = DateTime.UtcNow;
                int changes = await _context.SaveChangesAsync();
                return changes > 0;
            } catch (Exception ex) {
                Console.WriteLine($"[InvalidateSession] Error: {ex}");
                if (ex.InnerException != null) {
                    Console.WriteLine($"[InvalidateSession] Inner Exception: {ex.InnerException}");
                }
                return false;
            }
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

        public async Task<string?> GetUserEmailByTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token)) {
                return null;
            }

            var session = await _context.SessionToken
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Token == token);

            return session?.Customer?.Email;
        }
    }
}