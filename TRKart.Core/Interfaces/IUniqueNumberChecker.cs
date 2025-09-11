using System.Threading.Tasks;

namespace TRKart.Core.Interfaces
{
    public interface IUniqueNumberChecker
    {
        Task<bool> IsCardNumberUniqueAsync(string cardNumber);
        Task<bool> IsCustomerNumberUniqueAsync(string customerNumber);
        Task<bool> IsWalletNumberUniqueAsync(string walletNumber);
    }
}
