using Microsoft.EntityFrameworkCore;
using TRKart.Business.Interfaces;
using TRKart.Core.Helpers;
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
            // 1. Kullanıcıyı bul
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            // 2. Kullanıcı yoksa null döndür
            if (customer == null)
                return null;

            // 3. Şifre doğru mu kontrol et (BCrypt ile)
            bool isValid = BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash);
            if (!isValid)
                return null;

            // 4. JWT token üret
            string token = _jwtHelper.GenerateToken(customer.Email);

            // 5. SessionToken tablosuna kaydet (isteğe bağlı ama önerilir)
            var session = new SessionToken
            {
                CustomerId = customer.Id,
                Token = token,
                Expiration = DateTime.UtcNow.AddHours(1)
            };

            await _context.SessionTokens.AddAsync(session);
            await _context.SaveChangesAsync();

            // 6. Token'ı döndür
            return token;
        }

        public async Task<bool> RegisterAsync(RegisterDto dto)
        {
            // Aynı e-posta var mı kontrolü
            var existing = await _context.Customers.AnyAsync(x => x.Email == dto.Email);
            if (existing)
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
