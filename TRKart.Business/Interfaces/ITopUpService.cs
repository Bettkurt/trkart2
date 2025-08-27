using System.Threading.Tasks;
using TRKart.Entities.DTOs;
using TRKart.Entities.Models;

namespace TRKart.Business.Interfaces
{
    public interface ITopUpService
    {
        /// <summary>
        /// Processes a top-up request for a card
        /// </summary>
        /// <param name="request">Top-up request details</param>
        /// <param name="requestingCustomerId">ID of the customer making the request</param>
        /// <param name="correlationId">Correlation ID for logging and tracking</param>
        /// <returns>Top-up response with transaction details</returns>
        Task<TopUpResponseDto> TopUpAsync(TopUpRequestDto request, int requestingCustomerId, string correlationId);

        /// <summary>
        /// Validates a top-up request before processing
        /// </summary>
        /// <param name="request">Top-up request to validate</param>
        /// <param name="requestingCustomerId">ID of the customer making the request</param>
        /// <returns>Validation result with details</returns>
        Task<TopUpValidationDto> ValidateTopUpRequestAsync(TopUpRequestDto request, int requestingCustomerId);

        /// <summary>
        /// Updates the status of a pending top-up transaction (for webhook processing)
        /// </summary>
        /// <param name="statusUpdate">Status update details</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>Updated transaction or null if not found</returns>
        Task<Transaction?> UpdateTopUpStatusAsync(TopUpStatusUpdateDto statusUpdate, string correlationId);

        /// <summary>
        /// Processes a webhook notification for a top-up transaction
        /// </summary>
        /// <param name="webhook">Webhook payload</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>True if processed successfully, false otherwise</returns>
        Task<bool> ProcessTopUpWebhookAsync(TopUpWebhookDto webhook, string correlationId);

        /// <summary>
        /// Finds a top-up transaction by external reference
        /// </summary>
        /// <param name="externalRef">External reference to search for</param>
        /// <returns>Transaction if found, null otherwise</returns>
        Task<Transaction?> FindTopUpByExternalRefAsync(string externalRef);

        /// <summary>
        /// Simulates transaction approval for development/testing
        /// </summary>
        /// <param name="transactionId">ID of the transaction to approve</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>True if approved successfully, false otherwise</returns>
        Task<bool> SimulateTopUpApprovalAsync(int transactionId, string correlationId);

        /// <summary>
        /// Expires pending top-up transactions that have timed out
        /// </summary>
        /// <param name="timeoutMinutes">Number of minutes after which to expire pending transactions</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>Number of transactions expired</returns>
        Task<int> ExpirePendingTopUpsAsync(int timeoutMinutes, string correlationId);

        /// <summary>
        /// Gets top-up transactions for a specific customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <returns>List of top-up transactions</returns>
        Task<IEnumerable<TopUpTransactionDto>> GetCustomerTopUpsAsync(int customerId, int pageSize = 50, int pageNumber = 1);

        /// <summary>
        /// Checks if the requesting customer has permission to top up the target card
        /// </summary>
        /// <param name="targetCardNumber">Card number to top up</param>
        /// <param name="requestingCustomerId">ID of customer making the request</param>
        /// <returns>True if authorized, false otherwise</returns>
        Task<bool> IsAuthorizedForTopUpAsync(string targetCardNumber, int requestingCustomerId);
    }
}
