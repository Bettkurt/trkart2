using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TRKart.Entities.DTOs
{
    public class TransactionCreateDto
    {
        [Required(ErrorMessage = "CardID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "CardID must be a positive number")]
        public int CardID { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Amount must be between 0.01 and 999,999.99")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Amount must be a valid decimal number with up to 2 decimal places")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "TransactionType is required")]
        [RegularExpression(@"^(Pay|Load|Refund|TransferIn|TransferOut)$", ErrorMessage = "TransactionType must be one of: Pay, Load, Refund, TransferIn, TransferOut")]
        public string TransactionType { get; set; } = null!;

        [Required(ErrorMessage = "Description is required")]
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "Description can only contain letters and numbers. No special characters allowed.")]
        public string Description { get; set; } = null!;
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