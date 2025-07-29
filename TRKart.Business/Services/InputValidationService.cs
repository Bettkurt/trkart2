using System.Text.RegularExpressions;
using TRKart.Business.Interfaces;
using TRKart.Entities.DTOs;

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

            // Validate Amount
            var amountValidation = ValidateAmount(dto.Amount.ToString());
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

            // Try to parse as decimal
            if (!decimal.TryParse(amountString, out decimal amount))
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