using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TRKart.Entities.Enums;

namespace TRKart.Entities.DTOs
{
    public class TransactionCreateDto
    {
        public int? CardID { get; set; }
        public int? WalletId { get; set; }
        public string? ReferenceId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Amount must be between 0.01 and 999,999.99")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Amount must be a valid decimal number with up to 2 decimal places")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "TransactionType is required")]
        [EnumDataType(typeof(TransactionType), ErrorMessage = "Invalid transaction type")]
        public TransactionType TransactionType { get; set; }

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "Description can only contain letters and numbers. No special characters allowed.")]
        public string? Description { get; set; } = null!;

        public bool IsWalletTransaction => WalletId.HasValue;
    }

    public class TransactionFeasibilityResponse
    {
        public bool IsFeasible { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal ProjectedBalance { get; set; }
        public string Message { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
    }
} 