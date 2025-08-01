using System.Threading.Tasks;
using TRKart.Entities.DTOs;

namespace TRKart.Business.Interfaces
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterDto dto);
        Task<(string? Token, DateTime? ExpirationDate)> LoginAsync(LoginDto dto);
        Task<(bool IsValid, string? Email)> ValidateSessionAsync(string token);
        Task<bool> InvalidateSessionAsync(string token);
        Task<string?> GetUserEmailByTokenAsync(string token);
    }
}
