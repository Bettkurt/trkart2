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

        public async Task<(bool IsValid, string? Email, int? CustomerID, string? FullName)> ValidateSessionAsync(string token)
        {
            var session = await _context.SessionToken
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Token == token && s.Expiration > DateTime.UtcNow);
            
            if (session == null)
                return (false, null, null, null);
            
            return (true, session.Customer.Email, session.Customer.CustomerID, session.Customer.FullName);
        }

        public async Task<(string? Token, DateTime? Expiration)> LoginWithPasswordOnlyAsync(string token, string password)
        {
            // First validate the existing session
            var (isValid, email, customerID, fullName) = await ValidateSessionAsync(token);
            if (!isValid || string.IsNullOrEmpty(email))
                return (null, null);

            // Get customer and verify password
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Email == email);
            if (customer == null)
                return (null, null);

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, customer.PasswordHash);
            if (!isPasswordValid)
                return (null, null);

            // Generate new token and session
            string newToken = _jwtHelper.GenerateToken(customer.Email);
            DateTime expiration = DateTime.UtcNow.AddDays(7); // Extended session for password-only login

            // Remove old session and create new one
            var oldSession = await _context.SessionToken
                .FirstOrDefaultAsync(s => s.Token == token);
            if (oldSession != null)
            {
                _context.SessionToken.Remove(oldSession);
            }

            var newSession = new SessionToken
            {
                CustomerID = customer.CustomerID,
                Token = newToken,
                CreatedAt = DateTime.UtcNow,
                Expiration = expiration
            };

            await _context.SessionToken.AddAsync(newSession);
            await _context.SaveChangesAsync();

            return (newToken, expiration);
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

        public async Task<int?> GetCustomerIdFromTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            var session = await _context.SessionToken
                .FirstOrDefaultAsync(s => s.Token == token && s.Expiration > DateTime.UtcNow);
            
            return session?.CustomerID;
        }
    }
}