using Microsoft.EntityFrameworkCore;
using TRKart.Business.Interfaces;
using TRKart.Core.Helpers;
using TRKart.DataAccess;
using TRKart.Entities;
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

        public async Task<string?> LoginAsync(LoginDto dto)
        {
            // 1. Kullanıcıyı e-posta ile bul
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            // 2. Kullanıcı bulunamazsa null dön
            if (customer == null)
                return null;

            // 3. Şifre doğruluğunu kontrol et (BCrypt kullanılıyor)
            bool isValid = BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash);
            if (!isValid)
                return null;

            // 4. JWT token oluştur
            string token = _jwtHelper.GenerateToken(customer.Email);

            // 5. SessionToken tablosuna kaydet (isteğe bağlı ama güvenlik için önerilir)
            var session = new SessionToken
            {
                CustomerId = customer.Id,
                Token = token,
                Expiration = DateTime.UtcNow.AddHours(1)
            };

            await _context.SessionTokens.AddAsync(session);
            await _context.SaveChangesAsync();

            // 6. Token döndür
            return token;
        }

        public async Task<bool> RegisterAsync(RegisterDto dto)
        {
            // Aynı e-posta adresine sahip kullanıcı var mı kontrol et
            var exists = await _context.Customers.AnyAsync(x => x.Email == dto.Email);
            if (exists)
                return false;

            // Yeni kullanıcı oluştur
            var newCustomer = new Customer
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
