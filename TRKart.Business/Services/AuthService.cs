using TRKart.Business.Interfaces;
using TRKart.Entities.DTOs;
using TRKart.Core.Helpers;
using TRKart.DataAccess;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace TRKart.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly JwtHelper _jwtHelper;
        private readonly ApplicationDbContext _context;

        public AuthService(JwtHelper jwtHelper, ApplicationDbContext context)
        {
            _jwtHelper = jwtHelper;
            _context = context;
        }

        public async Task<bool> RegisterAsync(RegisterDto registerDto)
        {
            var existingUser = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == registerDto.Email);
            
            if (existingUser != null)
                return false;

        

            var customer = new TRKart.Entities.Customer
            {
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                 FullName = registerDto.FullName, 
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string?> LoginAsync(LoginDto loginDto)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == loginDto.Email);

            if (customer == null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, customer.PasswordHash))
                return null;

            return _jwtHelper.GenerateToken(loginDto.Email);
        }
    }
}
