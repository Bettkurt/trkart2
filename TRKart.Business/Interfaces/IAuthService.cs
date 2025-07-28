using System.Threading.Tasks;
using TRKart.Entities.DTOs;

namespace TRKart.Business.Interfaces
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterDto dto);
        Task<(string? Token, DateTime? Expiration)> LoginAsync(LoginDto dto);
    }
}
