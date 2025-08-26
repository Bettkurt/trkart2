using System.Threading.Tasks;

namespace TRKart.Business.Interfaces
{
    public interface ICardExpirationService
    {
        Task CheckAndUpdateExpiredCardsAsync();
    }
}
