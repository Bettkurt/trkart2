using System.Threading.Tasks;
using TRKart.Entities.DTOs;
using TRKart.Entities.Models;

namespace TRKart.Business.Interfaces
{
    public interface ITransferService
    {
        Task<TransferResponse> CreateTransferAsync(TransferCreateDto dto);
        Task<TransferValidationResponse> ValidateRecipientCardAsync(string cardNumber);
        Task<TransferResponse> GetTransferDetailsAsync(int transferTransactionID);
    }
} 