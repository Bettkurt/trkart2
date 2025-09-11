using System.Threading.Tasks;
using TRKart.Entities.DTOs;
using TRKart.Entities.Enums;
using TRKart.Entities.Models;

namespace TRKart.Business.Interfaces
{
    public interface IWalletService
    {
        Task<WalletDto> GetWalletByCustomerIdAsync(int customerId);
        Task<WalletDto> CreateWalletAsync(CreateWalletDto createWalletDto);
        Task<WalletDto> UpdateWalletStatusAsync(int walletId, CardStatus newStatus);
        
        /// <summary>
        /// Loads the wallet with the specified amount from the given card
        /// </summary>
        /// <param name="transactionDto">The wallet transaction details</param>
        /// <param name="isSystemTransfer">Indicates if this is a system-initiated transfer (e.g., from a blacklisted card)</param>
        /// <returns>The created transaction</returns>
        Task<Transaction> LoadWalletAsync(WalletTransactionDto transactionDto, bool isSystemTransfer = false);
        Task<Transaction> PayFromWalletAsync(WalletTransactionDto transactionDto);
        
        Task<decimal> GetWalletBalanceAsync(int walletId);
        
        /// <summary>
        /// Gets all transactions for a specific wallet
        /// </summary>
        /// <param name="walletId">The ID of the wallet</param>
        /// <returns>List of transactions for the wallet</returns>
        Task<List<WalletTransactionDto>> GetWalletTransactionsAsync(int walletId);
    }
}
