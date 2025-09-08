using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TRKart.Entities.Models;
using TRKart.Entities.Enums;

namespace TRKart.Entities.DTOs
{
    public class TopUpRequestDto
    {
        [Required(ErrorMessage = "Target card number is required")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Card number must be exactly 16 characters")]
        [RegularExpression(@"^[A-Z0-9]{16}$", ErrorMessage = "Card number must contain only uppercase letters and numbers")]
        public string TargetCardNumber { get; set; } = null!;

        [Required(ErrorMessage = "Amount is required")]
        [Range(10.00, 10000.00, ErrorMessage = "Amount must be between 10.00 and 10,000.00")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Amount must be a valid decimal number with up to 2 decimal places")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        [RegularExpression(@"^(CreditCard|Wire|Cash|BankTransfer|PayPal|Stripe)$", ErrorMessage = "Payment method must be one of: CreditCard, Wire, Cash, BankTransfer, PayPal, Stripe")]
        public string PaymentMethod { get; set; } = null!;

        [StringLength(100, ErrorMessage = "External reference cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "External reference can only contain letters, numbers, hyphens, and underscores")]
        public string? ExternalRef { get; set; }

        [Range(0.00, 1000.00, ErrorMessage = "Fee amount must be between 0.00 and 1,000.00")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Fee amount must be a valid decimal number with up to 2 decimal places")]
        public decimal? FeeAmount { get; set; }

        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters")]
        public string? Note { get; set; }
    }

    public class TopUpResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public TopUpTransactionDto? Transaction { get; set; }
        public string? Error { get; set; }
        public string? CorrelationId { get; set; }
    }

    public class TopUpTransactionDto
    {
        public int TransactionID { get; set; }
        public int CardID { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionType TransactionType { get; set; } = TransactionType.TopUp;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal? FeeAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string? ExternalRef { get; set; }
        public string? Note { get; set; }
        public DateTimeOffset TransactionDate { get; set; }
        public string TransactionStatus { get; set; } = string.Empty;
        public decimal? NewBalance { get; set; }
    }

    public class TopUpValidationDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? CardNumber { get; set; }
        public CardStatus? CardStatus { get; set; }
        public decimal? CurrentBalance { get; set; }
        public decimal? ProjectedBalance { get; set; }
        public bool DuplicateExternalRef { get; set; }
        public int? ExistingTransactionId { get; set; }
    }

    public class TopUpStatusUpdateDto
    {
        [Required(ErrorMessage = "Transaction ID is required")]
        public int TransactionId { get; set; }

        [Required(ErrorMessage = "New status is required")]
        [RegularExpression(@"^(Approved|Denied|Expired)$", ErrorMessage = "Status must be one of: Approved, Denied, Expired")]
        public string NewStatus { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string? Reason { get; set; }

        [StringLength(100, ErrorMessage = "Updated by cannot exceed 100 characters")]
        public string? UpdatedBy { get; set; }
    }

    // For webhook verification and processing
    public class TopUpWebhookDto
    {
        [Required(ErrorMessage = "External reference is required")]
        public string ExternalRef { get; set; } = null!;

        [Required(ErrorMessage = "Status is required")]
        [RegularExpression(@"^(Approved|Denied|Failed)$", ErrorMessage = "Status must be one of: Approved, Denied, Failed")]
        public string Status { get; set; } = null!;

        [Required(ErrorMessage = "Signature is required")]
        public string Signature { get; set; } = null!;

        [Required(ErrorMessage = "Timestamp is required")]
        public long Timestamp { get; set; }

        public string? ProviderTransactionId { get; set; }
        public string? FailureReason { get; set; }
        public decimal? ActualAmount { get; set; }
        public decimal? ActualFee { get; set; }
    }
}