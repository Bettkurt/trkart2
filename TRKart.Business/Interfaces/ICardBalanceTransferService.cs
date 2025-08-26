using System.Threading.Tasks;

namespace TRKart.Business.Interfaces
{
    public interface ICardBalanceTransferService
    {
        /// <summary>
        /// Transfers remaining balance from blacklisted cards to other active cards of the same user
        /// </summary>
        /// <returns>Number of successful balance transfers</returns>
        Task<int> TransferBalancesFromBlacklistedCardsAsync();
    }
}
