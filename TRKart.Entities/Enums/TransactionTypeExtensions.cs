using System;
using System.Collections.Generic;
using System.Linq;

namespace TRKart.Entities.Enums
{
    public static class TransactionTypeExtensions
    {
        private static readonly HashSet<TransactionType> UserCreatableTypes = new HashSet<TransactionType>
        {
            TransactionType.Load,        // For new-transaction page
            TransactionType.TopUp,       // For top-up page
            TransactionType.Refund,      // !!!Only for testing!!!
            TransactionType.TransferOut, // For new-transfer page
            TransactionType.Pay          // !!!Only for testing!!!
        };

        private static readonly HashSet<TransactionType> SystemOnlyTypes = new HashSet<TransactionType>
        {
            TransactionType.Refund,           // System-generated refunds
            TransactionType.TransferIn,       // Auto-created when TransferOut is made
            TransactionType.SystemTransferIn, // System balance transfers
            TransactionType.SystemTransferOut // System balance transfers
        };

        // Returns the transaction types that users can create/interact in the frontend
        public static IEnumerable<TransactionType> GetUserCreatableTransactionTypes()
        {
            return UserCreatableTypes.ToList();
        }

        // For now, users can see all transaction types in their transaction history
        // In the future, this can be limited to specific transaction types. However, doing that requires a HashSet of its own
        public static IEnumerable<TransactionType> GetUserViewableTransactionTypes()
        {
            return Enum.GetValues<TransactionType>().ToList();
        }

        public static bool IsUserCreatable(this TransactionType transactionType)
        {
            return UserCreatableTypes.Contains(transactionType);
        }

        // Returns true if the transaction type is only created by the system
        public static bool IsSystemOnly(this TransactionType transactionType)
        {
            return SystemOnlyTypes.Contains(transactionType);
        }

        // Returns the display name of the transaction type
        public static string GetDisplayName(this TransactionType transactionType)
        {
            return transactionType switch
            {
                TransactionType.Load => "Load",
                TransactionType.TopUp => "Top-Up",
                TransactionType.Refund => "Refund",
                TransactionType.TransferIn => "Incoming Transfer",
                TransactionType.TransferOut => "Outgoing Transfer",
                TransactionType.Pay => "Payment",
                TransactionType.SystemTransferIn => "System Transfer In",
                TransactionType.SystemTransferOut => "System Transfer Out",
                _ => "Unknown"
            };
        }

        // Returns true if the transaction type is a debit transaction (decreasing the balance)
        public static bool IsDebitTransaction(this TransactionType transactionType)
        {
            return transactionType == TransactionType.Pay || 
                   transactionType == TransactionType.TransferOut ||
                   transactionType == TransactionType.SystemTransferOut;
        }

        // Returns true if the transaction type is a credit transaction (increasing the balance)
        public static bool IsCreditTransaction(this TransactionType transactionType)
        {
            return transactionType == TransactionType.Load ||
                   transactionType == TransactionType.TopUp ||
                   transactionType == TransactionType.Refund ||
                   transactionType == TransactionType.TransferIn ||
                   transactionType == TransactionType.SystemTransferIn;
        }
    }
}
