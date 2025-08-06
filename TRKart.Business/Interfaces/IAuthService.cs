using TRKart.Entities.DTOs;

namespace TRKart.Business.Interfaces
{
    public interface IAuthService
    {
        Task<TokenResponse?> LoginAsync(LoginDto dto, string? ipAddress = null, string? deviceInfo = null);
        Task<TokenResponse?> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
        Task<(bool IsValid, string? Email, int? CustomerID, string? FullName)> ValidateAccessTokenAsync(string accessToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<bool> IsRefreshTokenBlacklistedAsync(string refreshToken);
        Task BlacklistRefreshTokenAsync(string refreshToken, string reason);
        Task<bool> RegisterAsync(RegisterDto dto);
        Task<int?> GetCustomerIdFromAccessTokenAsync(string accessToken);
        Task<string?> GetUserEmailByAccessTokenAsync(string accessToken);
    }
}