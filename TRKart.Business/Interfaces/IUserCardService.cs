using TRKart.Entities.DTOs;

namespace TRKart.Business.Interfaces
{
    public interface IUserCardService
    {
        Task<List<UserCardDto>> GetUserCardsAsync(int customerId);
        Task<bool> CreateUserCardAsync(UserCardDto dto);
    }
}