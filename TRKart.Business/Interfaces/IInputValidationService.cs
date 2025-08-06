using TRKart.Entities.DTOs;

namespace TRKart.Business.Interfaces
{
    public interface IInputValidationService
    {
        InputValidationResponse ValidateTransactionInput(TransactionCreateDto dto);
        InputValidationResponse ValidateAmount(string amountString);
        InputValidationResponse ValidateTransactionType(string transactionType);
        InputValidationResponse ValidateDescription(string description);
        InputValidationResponse ValidateCardId(string cardIdString);
    }
} 