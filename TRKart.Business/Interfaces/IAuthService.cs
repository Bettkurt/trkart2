using System.Threading.Tasks;
using TRKart.Entities.DTOs;

namespace TRKart.Business.Interfaces
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterDto dto);
        Task<(string? Token, DateTime? Expiration)> LoginAsync(LoginDto dto);
        Task<(bool IsValid, string? Email, int? CustomerID, string? FullName)> ValidateSessionAsync(string token);
        Task<(string? Token, DateTime? Expiration)> LoginWithPasswordOnlyAsync(string token, string password);
        Task<bool> InvalidateSessionAsync(string token);
        Task<string?> GetUserEmailByTokenAsync(string token);
        Task<int?> GetCustomerIdFromTokenAsync(string token);
    }
}
