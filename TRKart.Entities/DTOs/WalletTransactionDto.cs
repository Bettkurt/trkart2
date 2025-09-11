using System;
using System.ComponentModel.DataAnnotations;
using TRKart.Entities.Enums;

namespace TRKart.Entities.DTOs
{
    public class WalletTransactionDto
    {
        // These fields are set by the server, not the client
        public int TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; }
        
        // Required fields from client
        [Required(ErrorMessage = "Wallet ID is required")]
        public int WalletId { get; set; }
        
        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        // Optional fields
        public int? CardId { get; set; }
        
        [StringLength(50, ErrorMessage = "Transaction type cannot exceed 50 characters")]
        public string TransactionType { get; set; }
        
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string Description { get; set; }
        
        [StringLength(100, ErrorMessage = "Reference ID cannot exceed 100 characters")]
        public string ReferenceId { get; set; }
    }
}
