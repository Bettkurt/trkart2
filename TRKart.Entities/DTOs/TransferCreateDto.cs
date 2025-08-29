using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TRKart.Entities.Models;

namespace TRKart.Entities.DTOs
{
    public class TransferCreateDto
    {
        [Required(ErrorMessage = "SenderCardID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "SenderCardID must be a positive number")]
        public int SenderCardID { get; set; }

        [Required(ErrorMessage = "RecipientCardNumber is required")]
        [StringLength(16, ErrorMessage = "RecipientCardNumber must be 16 characters")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "RecipientCardNumber can only contain letters and numbers")]
        public string RecipientCardNumber { get; set; } = null!;

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Amount must be between 0.01 and 999,999.99")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Amount must be a valid decimal number with up to 2 decimal places")]
        public decimal Amount { get; set; }
    }

    public class TransferResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Transaction? TransferOutTransaction { get; set; }
        public Transaction? TransferInTransaction { get; set; }
        public string? Error { get; set; }
    }

    public class TransferValidationResponse
    {
        public bool Success { get; set; }
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; } = string.Empty;
        public UserCardDto? RecipientCard { get; set; }
    }
} 