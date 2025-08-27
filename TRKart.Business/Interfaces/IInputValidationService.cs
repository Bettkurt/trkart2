using TRKart.Entities.DTOs;
using TRKart.Entities.Models;

namespace TRKart.Business.Interfaces
{
    public interface IInputValidationService
    {
        InputValidationResponse ValidateTransactionInput(TransactionCreateDto dto);
        InputValidationResponse ValidateTransferInput(TransferCreateDto dto);
        InputValidationResponse ValidateTopUpInput(TopUpRequestDto dto);
        InputValidationResponse ValidateCreateUserCardInput(CreateUserCardDto dto);
        InputValidationResponse ValidateAmount(string amountString);
        InputValidationResponse ValidateTransactionType(string transactionType);
        InputValidationResponse ValidateDescription(string description);
        InputValidationResponse ValidateCardId(string cardIdString);
        InputValidationResponse ValidateCardNumber(string cardNumber);
        
        // Business logic validation methods
        InputValidationResponse ValidateTransferBusinessRules(TransferCreateDto dto, UserCard senderCard, UserCard recipientCard);
        InputValidationResponse ValidateTransactionFeasibility(TransactionCreateDto dto, decimal currentBalance);
    }
} 