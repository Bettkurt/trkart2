using System.Text.RegularExpressions;
using System.Globalization;
using TRKart.Business.Interfaces;
using TRKart.Entities.DTOs;
using TRKart.Entities.Models;

namespace TRKart.Business.Services
{
    public class InputValidationService : IInputValidationService
    {
        public InputValidationResponse ValidateTransactionInput(TransactionCreateDto dto)
        {
            var response = new InputValidationResponse();
            var errors = new List<ValidationError>();

            // Validate CardID
            var cardIdValidation = ValidateCardId(dto.CardID.ToString());
            if (!cardIdValidation.IsValid)
            {
                errors.AddRange(cardIdValidation.Errors);
            }

            // Validate Amount (use invariant culture to keep '.' as decimal separator)
            var amountValidation = ValidateAmount(dto.Amount.ToString(CultureInfo.InvariantCulture));
            if (!amountValidation.IsValid)
            {
                errors.AddRange(amountValidation.Errors);
            }

            // Validate TransactionType
            var transactionTypeValidation = ValidateTransactionType(dto.TransactionType);
            if (!transactionTypeValidation.IsValid)
            {
                errors.AddRange(transactionTypeValidation.Errors);
            }

            // Validate Description
            if (!string.IsNullOrEmpty(dto.Description))
            {
                var descriptionValidation = ValidateDescription(dto.Description);
                if (!descriptionValidation.IsValid)
                {
                    errors.AddRange(descriptionValidation.Errors);
                }
            }

            response.Errors = errors;
            response.IsValid = errors.Count == 0;
            response.Message = response.IsValid ? "Input validation passed" : "Input validation failed";

            return response;
        }

        public InputValidationResponse ValidateTransferInput(TransferCreateDto dto)
        {
            var response = new InputValidationResponse();
            var errors = new List<ValidationError>();

            // Validate SenderCardID
            var senderCardIdValidation = ValidateCardId(dto.SenderCardID.ToString());
            if (!senderCardIdValidation.IsValid)
            {
                errors.AddRange(senderCardIdValidation.Errors);
            }

            // Validate RecipientCardNumber
            var recipientCardNumberValidation = ValidateCardNumber(dto.RecipientCardNumber);
            if (!recipientCardNumberValidation.IsValid)
            {
                errors.AddRange(recipientCardNumberValidation.Errors);
            }

            // Validate Amount
            var amountValidation = ValidateAmount(dto.Amount.ToString());
            if (!amountValidation.IsValid)
            {
                errors.AddRange(amountValidation.Errors);
            }

            // Check if sender and recipient are the same
            if (dto.SenderCardID.ToString() == dto.RecipientCardNumber)
            {
                errors.Add(new ValidationError
                {
                    Field = "Transfer",
                    Error = "Sender and recipient cannot be the same",
                    Value = $"Sender: {dto.SenderCardID}, Recipient: {dto.RecipientCardNumber}"
                });
            }

            response.Errors = errors;
            response.IsValid = errors.Count == 0;
            response.Message = response.IsValid ? "Transfer input validation passed" : "Transfer input validation failed";

            return response;
        }

        public InputValidationResponse ValidateTopUpInput(TopUpRequestDto dto)
        {
            var response = new InputValidationResponse();
            var errors = new List<ValidationError>();

            // Validate TargetCardNumber
            if (string.IsNullOrWhiteSpace(dto.TargetCardNumber))
            {
                errors.Add(new ValidationError
                {
                    Field = "TargetCardNumber",
                    Error = "Target card number is required",
                    Value = dto.TargetCardNumber ?? "null"
                });
            }
            else
            {
                // Check length (exactly 16 characters as per TopUpRequestDto)
                if (dto.TargetCardNumber.Length != 16)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "TargetCardNumber",
                        Error = "Card number must be exactly 16 characters",
                        Value = dto.TargetCardNumber
                    });
                }

                // Check format (only uppercase letters and numbers as per TopUpRequestDto)
                if (!Regex.IsMatch(dto.TargetCardNumber, @"^[A-Z0-9]{16}$"))
                {
                    var invalidChars = Regex.Replace(dto.TargetCardNumber, @"[A-Z0-9]", "");
                    errors.Add(new ValidationError
                    {
                        Field = "TargetCardNumber",
                        Error = $"Card number contains invalid characters: {string.Join(", ", invalidChars.Distinct())}",
                        Value = dto.TargetCardNumber
                    });
                }
            }

            // Validate Amount (between 10.00 and 10,000.00 as per TopUpRequestDto)
            if (dto.Amount < 10.00m || dto.Amount > 10000.00m)
            {
                errors.Add(new ValidationError
                {
                    Field = "Amount",
                    Error = "Amount must be between 10.00 and 10,000.00",
                    Value = dto.Amount.ToString()
                });
            }

            // Validate PaymentMethod
            if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
            {
                errors.Add(new ValidationError
                {
                    Field = "PaymentMethod",
                    Error = "Payment method is required",
                    Value = dto.PaymentMethod ?? "null"
                });
            }
            else
            {
                var validPaymentMethods = new[] { "CreditCard", "Wire", "Cash", "BankTransfer", "PayPal", "Stripe" };
                if (!validPaymentMethods.Contains(dto.PaymentMethod))
                {
                    errors.Add(new ValidationError
                    {
                        Field = "PaymentMethod",
                        Error = $"Payment method must be one of: {string.Join(", ", validPaymentMethods)}",
                        Value = dto.PaymentMethod
                    });
                }
            }

            // Validate ExternalRef (optional, max 100 characters)
            if (!string.IsNullOrEmpty(dto.ExternalRef) && dto.ExternalRef.Length > 100)
            {
                errors.Add(new ValidationError
                {
                    Field = "ExternalRef",
                    Error = "External reference cannot exceed 100 characters",
                    Value = dto.ExternalRef
                });
            }

            // Validate FeeAmount (optional, between 0.00 and 1,000.00)
            if (dto.FeeAmount.HasValue)
            {
                if (dto.FeeAmount.Value < 0.00m || dto.FeeAmount.Value > 1000.00m)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "FeeAmount",
                        Error = "Fee amount must be between 0.00 and 1,000.00",
                        Value = dto.FeeAmount.Value.ToString()
                    });
                }
            }

            // Validate Note (optional, max 500 characters)
            if (!string.IsNullOrEmpty(dto.Note) && dto.Note.Length > 500)
            {
                errors.Add(new ValidationError
                {
                    Field = "Note",
                    Error = "Note cannot exceed 500 characters",
                    Value = dto.Note
                });
            }

            response.Errors = errors;
            response.IsValid = errors.Count == 0;
            response.Message = response.IsValid ? "Top-up input validation passed" : "Top-up input validation failed";

            return response;
        }

        public InputValidationResponse ValidateCreateUserCardInput(CreateUserCardDto dto)
        {
            var response = new InputValidationResponse();
            var errors = new List<ValidationError>();

            // Validate CustomerID
            if (dto.CustomerID <= 0)
            {
                errors.Add(new ValidationError
                {
                    Field = "CustomerID",
                    Error = "CustomerID must be a positive number",
                    Value = dto.CustomerID.ToString()
                });
            }

            // Validate CardName (optional, max 20 characters)
            if (!string.IsNullOrEmpty(dto.CardName) && dto.CardName.Length > 20)
            {
                errors.Add(new ValidationError
                {
                    Field = "CardName",
                    Error = "Card name cannot exceed 20 characters",
                    Value = dto.CardName
                });
            }

            // Check for invalid characters in CardName (only letters, numbers, spaces, and basic punctuation)
            if (!string.IsNullOrEmpty(dto.CardName) && !Regex.IsMatch(dto.CardName, @"^[a-zA-Z0-9\s\-_]+$"))
            {
                var invalidChars = Regex.Replace(dto.CardName, @"[a-zA-Z0-9\s\-_]", "");
                errors.Add(new ValidationError
                {
                    Field = "CardName",
                    Error = $"Card name contains invalid characters: {string.Join(", ", invalidChars.Distinct())}",
                    Value = dto.CardName
                });
            }

            response.Errors = errors;
            response.IsValid = errors.Count == 0;
            response.Message = response.IsValid ? "User card creation validation passed" : "User card creation validation failed";

            return response;
        }

        public InputValidationResponse ValidateCardNumber(string cardNumber)
        {
            var response = new InputValidationResponse();
            var errors = new List<ValidationError>();

            // Check for null or empty
            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                errors.Add(new ValidationError
                {
                    Field = "CardNumber",
                    Error = "Card number cannot be empty",
                    Value = cardNumber ?? "null"
                });
                response.Errors = errors;
                response.IsValid = false;
                response.Message = "Card number validation failed";
                return response;
            }

            // Check length (based on TransferCreateDto validation - between 8 and 50 characters)
            if (cardNumber.Length < 8 || cardNumber.Length > 50)
            {
                errors.Add(new ValidationError
                {
                    Field = "CardNumber",
                    Error = "Card number must be between 8 and 50 characters",
                    Value = cardNumber
                });
            }

            // Check for invalid characters (only letters and numbers allowed based on TransferCreateDto)
            if (!Regex.IsMatch(cardNumber, @"^[a-zA-Z0-9]+$"))
            {
                var invalidChars = Regex.Replace(cardNumber, @"[a-zA-Z0-9]", "");
                errors.Add(new ValidationError
                {
                    Field = "CardNumber",
                    Error = $"Card number contains invalid characters: {string.Join(", ", invalidChars.Distinct())}",
                    Value = cardNumber
                });
            }

            response.Errors = errors;
            response.IsValid = errors.Count == 0;
            response.Message = response.IsValid ? "Card number validation passed" : "Card number validation failed";

            return response;
        }

        public InputValidationResponse ValidateTransferBusinessRules(TransferCreateDto dto, UserCard senderCard, UserCard recipientCard)
        {
            var response = new InputValidationResponse();
            var errors = new List<ValidationError>();

            // Validate sender card exists
            if (senderCard == null)
            {
                errors.Add(new ValidationError
                {
                    Field = "SenderCard",
                    Error = "Sender card not found",
                    Value = dto.SenderCardID.ToString()
                });
            }

            // Validate recipient card exists
            if (recipientCard == null)
            {
                errors.Add(new ValidationError
                {
                    Field = "RecipientCard",
                    Error = "Recipient card not found",
                    Value = dto.RecipientCardNumber
                });
            }

            // Validate sender has sufficient balance
            if (senderCard != null && senderCard.Balance < dto.Amount)
            {
                errors.Add(new ValidationError
                {
                    Field = "Balance",
                    Error = "Insufficient balance for transfer",
                    Value = $"Current: {senderCard.Balance}, Required: {dto.Amount}"
                });
            }

            // Validate recipient card status is Active
            if (recipientCard != null && recipientCard.CardStatus != "Active")
            {
                errors.Add(new ValidationError
                {
                    Field = "RecipientCardStatus",
                    Error = "Transfer cannot be completed. Card not found.",
                    Value = recipientCard.CardStatus
                });
            }

            response.Errors = errors;
            response.IsValid = errors.Count == 0;
            response.Message = response.IsValid ? "Transfer business rules validation passed" : "Transfer business rules validation failed";

            return response;
        }

        public InputValidationResponse ValidateTransactionFeasibility(TransactionCreateDto dto, decimal currentBalance)
        {
            var response = new InputValidationResponse();
            var errors = new List<ValidationError>();

            // Calculate projected balance based on transaction type
            decimal projectedBalance = currentBalance;
            
            switch (dto.TransactionType.ToLower())
            {
                case "transferout":
                case "pay":
                    projectedBalance -= dto.Amount;
                    break;
                case "load":
                case "transferin":
                case "refund":
                    projectedBalance += dto.Amount;
                    break;
                default:
                    errors.Add(new ValidationError
                    {
                        Field = "TransactionType",
                        Error = $"Invalid transaction type: {dto.TransactionType}",
                        Value = dto.TransactionType
                    });
                    response.Errors = errors;
                    response.IsValid = false;
                    response.Message = "Transaction feasibility validation failed";
                    return response;
            }

            // Check if transaction would result in negative balance
            if (projectedBalance < 0)
            {
                errors.Add(new ValidationError
                {
                    Field = "Balance",
                    Error = "Insufficient funds for transaction",
                    Value = $"Current: {currentBalance:C}, Required: {dto.Amount:C}, Projected: {projectedBalance:C}"
                });
            }

            // Check for reasonable transaction amount
            if (dto.Amount <= 0)
            {
                errors.Add(new ValidationError
                {
                    Field = "Amount",
                    Error = "Transaction amount must be greater than zero",
                    Value = dto.Amount.ToString()
                });
            }

            response.Errors = errors;
            response.IsValid = errors.Count == 0;
            response.Message = response.IsValid ? "Transaction feasibility validation passed" : "Transaction feasibility validation failed";

            return response;
        }

        public InputValidationResponse ValidateAmount(string amountString)
        {
            var response = new InputValidationResponse();
            var errors = new List<ValidationError>();

            // Check for null or empty
            if (string.IsNullOrWhiteSpace(amountString))
            {
                errors.Add(new ValidationError
                {
                    Field = "Amount",
                    Error = "Amount cannot be empty",
                    Value = amountString ?? "null"
                });
                response.Errors = errors;
                response.IsValid = false;
                response.Message = "Amount validation failed";
                return response;
            }

            // Check for invalid characters (only numbers, decimal point, and minus sign allowed)
            if (!Regex.IsMatch(amountString, @"^[0-9.-]+$"))
            {
                var invalidChars = Regex.Replace(amountString, @"[0-9.-]", "");
                errors.Add(new ValidationError
                {
                    Field = "Amount",
                    Error = $"Amount contains invalid characters: {string.Join(", ", invalidChars.Distinct())}",
                    Value = amountString
                });
            }

            // Check for multiple decimal points
            if (amountString.Count(c => c == '.') > 1)
            {
                errors.Add(new ValidationError
                {
                    Field = "Amount",
                    Error = "Amount cannot contain multiple decimal points",
                    Value = amountString
                });
            }

            // Check for multiple minus signs
            if (amountString.Count(c => c == '-') > 1)
            {
                errors.Add(new ValidationError
                {
                    Field = "Amount",
                    Error = "Amount cannot contain multiple minus signs",
                    Value = amountString
                });
            }

            // Check for minus sign not at the beginning
            if (amountString.Contains('-') && !amountString.StartsWith('-'))
            {
                errors.Add(new ValidationError
                {
                    Field = "Amount",
                    Error = "Minus sign must be at the beginning of the amount",
                    Value = amountString
                });
            }

            // Check for decimal places (max 2)
            if (amountString.Contains('.'))
            {
                var decimalPlaces = amountString.Split('.')[1].Length;
                if (decimalPlaces > 2)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "Amount",
                        Error = "Amount cannot have more than 2 decimal places",
                        Value = amountString
                    });
                }
            }

            // Try to parse as decimal using invariant culture (expects '.' as decimal separator)
            if (!decimal.TryParse(amountString, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out decimal amount))
            {
                errors.Add(new ValidationError
                {
                    Field = "Amount",
                    Error = "Amount is not a valid decimal number",
                    Value = amountString
                });
            }
            else
            {
                // Check range
                if (amount <= 0)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "Amount",
                        Error = "Amount must be greater than zero",
                        Value = amountString
                    });
                }
                else if (amount > 999999.99m)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "Amount",
                        Error = "Amount cannot exceed 999,999.99",
                        Value = amountString
                    });
                }
            }

            response.Errors = errors;
            response.IsValid = errors.Count == 0;
            response.Message = response.IsValid ? "Amount validation passed" : "Amount validation failed";

            return response;
        }

        public InputValidationResponse ValidateTransactionType(string transactionType)
        {
            var response = new InputValidationResponse();
            var errors = new List<ValidationError>();

            // Check for null or empty
            if (string.IsNullOrWhiteSpace(transactionType))
            {
                errors.Add(new ValidationError
                {
                    Field = "TransactionType",
                    Error = "TransactionType cannot be empty",
                    Value = transactionType ?? "null"
                });
                response.Errors = errors;
                response.IsValid = false;
                response.Message = "TransactionType validation failed";
                return response;
            }

            // Check for invalid characters (only letters allowed)
            if (!Regex.IsMatch(transactionType, @"^[a-zA-Z]+$"))
            {
                var invalidChars = Regex.Replace(transactionType, @"[a-zA-Z]", "");
                errors.Add(new ValidationError
                {
                    Field = "TransactionType",
                    Error = $"TransactionType contains invalid characters: {string.Join(", ", invalidChars.Distinct())}",
                    Value = transactionType
                });
            }

            // Check for valid transaction types
            var validTypes = new[] { "Pay", "Load", "Transfer", "Refund" };
            if (!validTypes.Contains(transactionType, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(new ValidationError
                {
                    Field = "TransactionType",
                    Error = $"TransactionType must be one of: {string.Join(", ", validTypes)}",
                    Value = transactionType
                });
            }

            response.Errors = errors;
            response.IsValid = errors.Count == 0;
            response.Message = response.IsValid ? "TransactionType validation passed" : "TransactionType validation failed";

            return response;
        }

        public InputValidationResponse ValidateDescription(string description)
        {
            var response = new InputValidationResponse();
            var errors = new List<ValidationError>();

            // Check for null (description is optional)
            if (description == null)
            {
                response.Errors = errors;
                response.IsValid = true;
                response.Message = "Description validation passed (null is allowed)";
                return response;
            }

            // Check length
            if (description.Length > 500)
            {
                errors.Add(new ValidationError
                {
                    Field = "Description",
                    Error = "Description cannot exceed 500 characters",
                    Value = description
                });
            }

            // Check for invalid characters (only letters, numbers, spaces, and basic punctuation)
            if (!Regex.IsMatch(description, @"^[a-zA-Z0-9\s\-_.,!?()]+$"))
            {
                var invalidChars = Regex.Replace(description, @"[a-zA-Z0-9\s\-_.,!?()]", "");
                errors.Add(new ValidationError
                {
                    Field = "Description",
                    Error = $"Description contains invalid characters: {string.Join(", ", invalidChars.Distinct())}",
                    Value = description
                });
            }

            response.Errors = errors;
            response.IsValid = errors.Count == 0;
            response.Message = response.IsValid ? "Description validation passed" : "Description validation failed";

            return response;
        }

        public InputValidationResponse ValidateCardId(string cardIdString)
        {
            var response = new InputValidationResponse();
            var errors = new List<ValidationError>();

            // Check for null or empty
            if (string.IsNullOrWhiteSpace(cardIdString))
            {
                errors.Add(new ValidationError
                {
                    Field = "CardID",
                    Error = "CardID cannot be empty",
                    Value = cardIdString ?? "null"
                });
                response.Errors = errors;
                response.IsValid = false;
                response.Message = "CardID validation failed";
                return response;
            }

            // Check for invalid characters (only numbers allowed)
            if (!Regex.IsMatch(cardIdString, @"^[0-9]+$"))
            {
                var invalidChars = Regex.Replace(cardIdString, @"[0-9]", "");
                errors.Add(new ValidationError
                {
                    Field = "CardID",
                    Error = $"CardID contains invalid characters: {string.Join(", ", invalidChars.Distinct())}",
                    Value = cardIdString
                });
            }

            // Try to parse as integer
            if (!int.TryParse(cardIdString, out int cardId))
            {
                errors.Add(new ValidationError
                {
                    Field = "CardID",
                    Error = "CardID is not a valid integer",
                    Value = cardIdString
                });
            }
            else
            {
                // Check range
                if (cardId <= 0)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "CardID",
                        Error = "CardID must be a positive number",
                        Value = cardIdString
                    });
                }
            }

            response.Errors = errors;
            response.IsValid = errors.Count == 0;
            response.Message = response.IsValid ? "CardID validation passed" : "CardID validation failed";

            return response;
        }
    }
} 