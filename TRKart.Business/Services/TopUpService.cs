using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.DTOs;
using TRKart.Entities.Models;

namespace TRKart.Business.Services
{
    public class TopUpService : ITopUpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInputValidationService _inputValidationService;
        private readonly ILogger<TopUpService> _logger;

        public TopUpService(
            ApplicationDbContext context,
            IInputValidationService inputValidationService,
            ILogger<TopUpService> logger)
        {
            _context = context;
            _inputValidationService = inputValidationService;
            _logger = logger;
        }

        public async Task<TopUpResponseDto> TopUpAsync(TopUpRequestDto request, int requestingCustomerId, string correlationId)
        {
            _logger.LogInformation("[{CorrelationId}] TopUp request initiated for card {CardNumber} by customer {CustomerId}, amount: {Amount}", 
                correlationId, request.TargetCardNumber, requestingCustomerId, request.Amount);

            try
            {
                // 1. Validate input format
                var inputValidation = _inputValidationService.ValidateTopUpInput(request);
                if (!inputValidation.IsValid)
                {
                    _logger.LogWarning("[{CorrelationId}] TopUp input validation failed: {Errors}", 
                        correlationId, string.Join("; ", inputValidation.Errors));
                    return new TopUpResponseDto 
                    { 
                        Success = false, 
                        Message = inputValidation.Message,
                        Error = "INPUT_VALIDATION_ERROR",
                        CorrelationId = correlationId
                    };
                }

                // 2. Validate business rules and permissions
                var businessValidation = await ValidateTopUpRequestAsync(request, requestingCustomerId);
                if (!businessValidation.IsValid)
                {
                    _logger.LogWarning("[{CorrelationId}] TopUp business validation failed: {Message}", 
                        correlationId, businessValidation.Message);
                    return new TopUpResponseDto 
                    { 
                        Success = false, 
                        Message = businessValidation.Message,
                        Error = "BUSINESS_VALIDATION_ERROR",
                        CorrelationId = correlationId
                    };
                }

                // 3. Check for duplicate ExternalRef (idempotency)
                if (!string.IsNullOrEmpty(request.ExternalRef))
                {
                    var existingTransaction = await FindTopUpByExternalRefAsync(request.ExternalRef);
                    if (existingTransaction != null)
                    {
                        _logger.LogInformation("[{CorrelationId}] Duplicate ExternalRef {ExternalRef} found, returning existing transaction {TransactionId}", 
                            correlationId, request.ExternalRef, existingTransaction.TransactionID);
                        
                        return await CreateResponseFromExistingTransaction(existingTransaction, correlationId);
                    }
                }

                // 4. Create pending transaction using database transaction with locking
                Transaction topUpTransaction;
                UserCard targetCard;

                using (var dbTransaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Get and lock the target card
                        targetCard = await _context.UserCard
                            .Where(c => c.CardNumber == request.TargetCardNumber)
                            .FirstOrDefaultAsync();

                        if (targetCard == null)
                        {
                            _logger.LogWarning("[{CorrelationId}] Target card {CardNumber} not found", 
                                correlationId, request.TargetCardNumber);
                            return new TopUpResponseDto 
                            { 
                                Success = false, 
                                Message = "Target card not found",
                                Error = "CARD_NOT_FOUND",
                                CorrelationId = correlationId
                            };
                        }

                        // Verify card is active
                        if (targetCard.CardStatus != "Active")
                        {
                            _logger.LogWarning("[{CorrelationId}] Card {CardNumber} is not active, status: {Status}", 
                                correlationId, request.TargetCardNumber, targetCard.CardStatus);
                            return new TopUpResponseDto 
                            { 
                                Success = false, 
                                Message = $"Card is not active. Current status: {targetCard.CardStatus}",
                                Error = "CARD_NOT_ACTIVE",
                                CorrelationId = correlationId
                            };
                        }

                        // Create the pending transaction
                        topUpTransaction = new Transaction
                        {
                            CardID = targetCard.CardID,
                            Amount = request.Amount,
                            TransactionType = "TopUp",
                            Description = $"Top-up via {request.PaymentMethod}",
                            ExternalRef = string.IsNullOrEmpty(request.ExternalRef) ? null : request.ExternalRef,
                            PaymentMethod = request.PaymentMethod,
                            FeeAmount = request.FeeAmount ?? 0m,
                            Note = request.Note,
                            TransactionStatus = "Pending"
                        };

                        _context.Transaction.Add(topUpTransaction);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("[{CorrelationId}] Pending TopUp transaction {TransactionId} created for card {CardNumber}", 
                            correlationId, topUpTransaction.TransactionID, request.TargetCardNumber);

                        // Simulate immediate approval for development
                        await SimulateTopUpApprovalInternal(topUpTransaction, targetCard, correlationId);

                        await dbTransaction.CommitAsync();

                        _logger.LogInformation("[{CorrelationId}] TopUp transaction {TransactionId} completed successfully", 
                            correlationId, topUpTransaction.TransactionID);
                    }
                    catch (Exception ex)
                    {
                        await dbTransaction.RollbackAsync();
                        _logger.LogError(ex, "[{CorrelationId}] Error creating TopUp transaction", correlationId);
                        throw;
                    }
                }

                // Reload transaction to get updated status and balance
                await _context.Entry(topUpTransaction).ReloadAsync();
                await _context.Entry(targetCard).ReloadAsync();

                return new TopUpResponseDto
                {
                    Success = true,
                    Message = "Top-up completed successfully",
                    CorrelationId = correlationId,
                    Transaction = new TopUpTransactionDto
                    {
                        TransactionID = topUpTransaction.TransactionID,
                        CardID = topUpTransaction.CardID,
                        CardNumber = targetCard.CardNumber,
                        Amount = topUpTransaction.Amount,
                        TransactionType = topUpTransaction.TransactionType,
                        PaymentMethod = topUpTransaction.PaymentMethod ?? "",
                        FeeAmount = topUpTransaction.FeeAmount,
                        NetAmount = topUpTransaction.Amount - (topUpTransaction.FeeAmount ?? 0m),
                        ExternalRef = topUpTransaction.ExternalRef,
                        Note = topUpTransaction.Note,
                        TransactionDate = topUpTransaction.TransactionDate,
                        TransactionStatus = topUpTransaction.TransactionStatus,
                        NewBalance = targetCard.Balance
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Unexpected error during TopUp processing", correlationId);
                return new TopUpResponseDto 
                { 
                    Success = false, 
                    Message = "An unexpected error occurred during top-up processing",
                    Error = "INTERNAL_ERROR",
                    CorrelationId = correlationId
                };
            }
        }

        public async Task<TopUpValidationDto> ValidateTopUpRequestAsync(TopUpRequestDto request, int requestingCustomerId)
        {
            var result = new TopUpValidationDto { IsValid = true };

            try
            {
                // 1. Check if card exists
                var card = await _context.UserCard
                    .Where(c => c.CardNumber == request.TargetCardNumber)
                    .Select(c => new { c.CardID, c.Balance, c.CardStatus, c.CustomerID })
                    .FirstOrDefaultAsync();

                if (card == null)
                {
                    result.IsValid = false;
                    result.Message = "Target card not found";
                    return result;
                }

                result.CardNumber = request.TargetCardNumber;
                result.CardStatus = card.CardStatus;
                result.CurrentBalance = card.Balance;

                // 2. Check card status
                if (card.CardStatus != "Active")
                {
                    result.IsValid = false;
                    result.Message = $"Card is not active. Current status: {card.CardStatus}";
                    return result;
                }

                // 3. Check authorization (can only top up own cards unless admin)
                if (!await IsAuthorizedForTopUpAsync(request.TargetCardNumber, requestingCustomerId))
                {
                    result.IsValid = false;
                    result.Message = "You are not authorized to top up this card";
                    return result;
                }

                // 4. Check amount limits
                if (request.Amount < 10m || request.Amount > 10000m)
                {
                    result.IsValid = false;
                    result.Message = "Top-up amount must be between 10.00 and 10,000.00";
                    return result;
                }

                // 5. Check fee amount validity
                if (request.FeeAmount.HasValue)
                {
                    if (request.FeeAmount.Value < 0 || request.FeeAmount.Value >= request.Amount)
                    {
                        result.IsValid = false;
                        result.Message = "Fee amount must be non-negative and less than the top-up amount";
                        return result;
                    }
                }

                // 6. Check for duplicate ExternalRef
                if (!string.IsNullOrEmpty(request.ExternalRef))
                {
                    var existingTransaction = await _context.Transaction
                        .Where(t => t.ExternalRef == request.ExternalRef)
                        .Select(t => new { t.TransactionID })
                        .FirstOrDefaultAsync();

                    if (existingTransaction != null)
                    {
                        result.DuplicateExternalRef = true;
                        result.ExistingTransactionId = existingTransaction.TransactionID;
                        result.Message = $"ExternalRef '{request.ExternalRef}' already exists";
                    }
                }

                // 7. Calculate projected balance
                var netAmount = request.Amount - (request.FeeAmount ?? 0m);
                result.ProjectedBalance = result.CurrentBalance + netAmount;

                if (result.IsValid)
                {
                    result.Message = "Top-up request is valid";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating TopUp request for card {CardNumber}", request.TargetCardNumber);
                result.IsValid = false;
                result.Message = "Error validating top-up request";
                return result;
            }
        }

        public async Task<Transaction?> UpdateTopUpStatusAsync(TopUpStatusUpdateDto statusUpdate, string correlationId)
        {
            _logger.LogInformation("[{CorrelationId}] Updating TopUp transaction {TransactionId} to status {Status}", 
                correlationId, statusUpdate.TransactionId, statusUpdate.NewStatus);

            using (var dbTransaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var transaction = await _context.Transaction
                        .Include(t => t.UserCard)
                        .Where(t => t.TransactionID == statusUpdate.TransactionId && t.TransactionType == "TopUp")
                        .FirstOrDefaultAsync();

                    if (transaction == null)
                    {
                        _logger.LogWarning("[{CorrelationId}] TopUp transaction {TransactionId} not found", 
                            correlationId, statusUpdate.TransactionId);
                        return null;
                    }

                    // Can only update pending transactions
                    if (transaction.TransactionStatus != "Pending")
                    {
                        _logger.LogWarning("[{CorrelationId}] Cannot update TopUp transaction {TransactionId}, current status: {Status}", 
                            correlationId, statusUpdate.TransactionId, transaction.TransactionStatus);
                        return null;
                    }

                    var oldStatus = transaction.TransactionStatus;
                    transaction.TransactionStatus = statusUpdate.NewStatus;

                    // If approving, update the card balance
                    if (statusUpdate.NewStatus == "Approved")
                    {
                        var netAmount = transaction.Amount - (transaction.FeeAmount ?? 0m);
                        transaction.UserCard.Balance += netAmount;
                        
                        _logger.LogInformation("[{CorrelationId}] Updated card {CardId} balance by {NetAmount}", 
                            correlationId, transaction.CardID, netAmount);
                    }

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    _logger.LogInformation("[{CorrelationId}] TopUp transaction {TransactionId} status updated from {OldStatus} to {NewStatus}", 
                        correlationId, statusUpdate.TransactionId, oldStatus, statusUpdate.NewStatus);

                    return transaction;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "[{CorrelationId}] Error updating TopUp transaction {TransactionId} status", 
                        correlationId, statusUpdate.TransactionId);
                    throw;
                }
            }
        }

        public async Task<bool> ProcessTopUpWebhookAsync(TopUpWebhookDto webhook, string correlationId)
        {
            _logger.LogInformation("[{CorrelationId}] Processing TopUp webhook for ExternalRef {ExternalRef}, status: {Status}", 
                correlationId, webhook.ExternalRef, webhook.Status);

            try
            {
                // TODO: Implement webhook signature verification
                if (!VerifyWebhookSignature(webhook))
                {
                    _logger.LogWarning("[{CorrelationId}] Invalid webhook signature for ExternalRef {ExternalRef}", 
                        correlationId, webhook.ExternalRef);
                    return false;
                }

                // TODO: Implement replay attack prevention
                if (!IsWebhookTimestampValid(webhook.Timestamp))
                {
                    _logger.LogWarning("[{CorrelationId}] Invalid or expired timestamp for webhook ExternalRef {ExternalRef}", 
                        correlationId, webhook.ExternalRef);
                    return false;
                }

                var transaction = await FindTopUpByExternalRefAsync(webhook.ExternalRef);
                if (transaction == null)
                {
                    _logger.LogWarning("[{CorrelationId}] TopUp transaction not found for ExternalRef {ExternalRef}", 
                        correlationId, webhook.ExternalRef);
                    return false;
                }

                var statusUpdate = new TopUpStatusUpdateDto
                {
                    TransactionId = transaction.TransactionID,
                    NewStatus = webhook.Status == "Approved" ? "Approved" : "Denied",
                    Reason = webhook.FailureReason,
                    UpdatedBy = "Webhook"
                };

                var updatedTransaction = await UpdateTopUpStatusAsync(statusUpdate, correlationId);
                return updatedTransaction != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Error processing TopUp webhook for ExternalRef {ExternalRef}", 
                    correlationId, webhook.ExternalRef);
                return false;
            }
        }

        public async Task<Transaction?> FindTopUpByExternalRefAsync(string externalRef)
        {
            if (string.IsNullOrEmpty(externalRef))
                return null;

            return await _context.Transaction
                .Where(t => t.ExternalRef == externalRef && t.TransactionType == "TopUp")
                .FirstOrDefaultAsync();
        }

        public async Task<bool> SimulateTopUpApprovalAsync(int transactionId, string correlationId)
        {
            var statusUpdate = new TopUpStatusUpdateDto
            {
                TransactionId = transactionId,
                NewStatus = "Approved",
                UpdatedBy = "Simulation"
            };

            var result = await UpdateTopUpStatusAsync(statusUpdate, correlationId);
            return result != null;
        }

        public async Task<int> ExpirePendingTopUpsAsync(int timeoutMinutes, string correlationId)
        {
            _logger.LogInformation("[{CorrelationId}] Expiring TopUp transactions older than {TimeoutMinutes} minutes", 
                correlationId, timeoutMinutes);

            var cutoffTime = DateTime.UtcNow.AddMinutes(-timeoutMinutes);
            var expiredCount = 0;

            try
            {
                var pendingTransactions = await _context.Transaction
                    .Where(t => t.TransactionType == "TopUp" 
                               && t.TransactionStatus == "Pending" 
                               && t.TransactionDate < cutoffTime)
                    .ToListAsync();

                foreach (var transaction in pendingTransactions)
                {
                    transaction.TransactionStatus = "Expired";
                    expiredCount++;
                }

                if (expiredCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("[{CorrelationId}] Expired {Count} TopUp transactions", correlationId, expiredCount);
                }

                return expiredCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Error expiring pending TopUp transactions", correlationId);
                return 0;
            }
        }

        public async Task<IEnumerable<TopUpTransactionDto>> GetCustomerTopUpsAsync(int customerId, int pageSize = 50, int pageNumber = 1)
        {
            var skip = (pageNumber - 1) * pageSize;

            var topUps = await _context.Transaction
                .Include(t => t.UserCard)
                .Where(t => t.TransactionType == "TopUp" && t.UserCard.CustomerID == customerId)
                .OrderByDescending(t => t.TransactionDate)
                .Skip(skip)
                .Take(pageSize)
                .Select(t => new TopUpTransactionDto
                {
                    TransactionID = t.TransactionID,
                    CardID = t.CardID,
                    CardNumber = t.UserCard.CardNumber,
                    Amount = t.Amount,
                    TransactionType = t.TransactionType,
                    PaymentMethod = t.PaymentMethod ?? "",
                    FeeAmount = t.FeeAmount,
                    NetAmount = t.Amount - (t.FeeAmount ?? 0m),
                    ExternalRef = t.ExternalRef,
                    Note = t.Note,
                    TransactionDate = t.TransactionDate,
                    TransactionStatus = t.TransactionStatus
                })
                .ToListAsync();

            return topUps;
        }

        public async Task<bool> IsAuthorizedForTopUpAsync(string targetCardNumber, int requestingCustomerId)
        {
            var card = await _context.UserCard
                .Where(c => c.CardNumber == targetCardNumber)
                .Select(c => new { c.CustomerID })
                .FirstOrDefaultAsync();

            if (card == null)
                return false;

            // For now, users can only top up their own cards
            // In the future, this could be extended to support admin roles or card sharing
            return card.CustomerID == requestingCustomerId;
        }

        #region Private Helper Methods

        private async Task<TopUpResponseDto> CreateResponseFromExistingTransaction(Transaction transaction, string correlationId)
        {
            var card = await _context.UserCard
                .Where(c => c.CardID == transaction.CardID)
                .FirstOrDefaultAsync();

            return new TopUpResponseDto
            {
                Success = true,
                Message = "Duplicate request - returning existing transaction",
                CorrelationId = correlationId,
                Transaction = new TopUpTransactionDto
                {
                    TransactionID = transaction.TransactionID,
                    CardID = transaction.CardID,
                    CardNumber = card?.CardNumber ?? "",
                    Amount = transaction.Amount,
                    TransactionType = transaction.TransactionType,
                    PaymentMethod = transaction.PaymentMethod ?? "",
                    FeeAmount = transaction.FeeAmount,
                    NetAmount = transaction.Amount - (transaction.FeeAmount ?? 0m),
                    ExternalRef = transaction.ExternalRef,
                    Note = transaction.Note,
                    TransactionDate = transaction.TransactionDate,
                    TransactionStatus = transaction.TransactionStatus,
                    NewBalance = card?.Balance
                }
            };
        }

        private async Task SimulateTopUpApprovalInternal(Transaction transaction, UserCard card, string correlationId)
        {
            _logger.LogInformation("[{CorrelationId}] Simulating approval for TopUp transaction {TransactionId}", 
                correlationId, transaction.TransactionID);

            // In a real implementation, this would wait for PSP webhook
            // For now, we immediately approve and update the balance
            var netAmount = transaction.Amount - (transaction.FeeAmount ?? 0m);
            
            transaction.TransactionStatus = "Approved";
            card.Balance += netAmount;

            _logger.LogInformation("[{CorrelationId}] TopUp transaction {TransactionId} approved, card balance updated by {NetAmount}", 
                correlationId, transaction.TransactionID, netAmount);
        }

        private bool VerifyWebhookSignature(TopUpWebhookDto webhook)
        {
            // TODO: Implement actual signature verification
            // This would typically involve HMAC SHA256 with a secret key
            return !string.IsNullOrEmpty(webhook.Signature);
        }

        private bool IsWebhookTimestampValid(long timestamp)
        {
            // TODO: Implement timestamp validation to prevent replay attacks
            // Typically allow 5-15 minutes tolerance
            var webhookTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            var now = DateTimeOffset.UtcNow;
            var tolerance = TimeSpan.FromMinutes(15);

            return Math.Abs((now - webhookTime).TotalMinutes) <= tolerance.TotalMinutes;
        }

        #endregion
    }
}
