using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TRKart.DataAccess;
using TRKart.Entities;
using TRKart.Entities.Models;

namespace TRKart.API.Services
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupService> _logger;
        private const int BatchSize = 1000;
        private readonly int _deleteOlderThanDays;
        private readonly int _cleanupIntervalDays;

        public TokenCleanupService(
            IServiceProvider serviceProvider, 
            ILogger<TokenCleanupService> logger,
            IOptions<TokenCleanupSettings> settings)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var settingsValue = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            
            _deleteOlderThanDays = settingsValue.DeleteOlderThanDays;
            _cleanupIntervalDays = settingsValue.CleanupIntervalDays;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Service is starting with interval: {Interval} days", _cleanupIntervalDays);

            // Initial delay to prevent blocking application startup
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting scheduled token cleanup {UtcNow}", DateTime.UtcNow);
                    await CleanupTokensAsync(stoppingToken);
                    _logger.LogInformation("Completed token cleanup {UtcNow}", DateTime.UtcNow);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Token cleanup was cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while cleaning up tokens");
                }

                try
                {
                    await Task.Delay(
                        //a Must be FromDays for prod, FromMinutes for testing
                        TimeSpan.FromDays(_cleanupIntervalDays), 
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Token cleanup service is stopping");
                    break;
                }
            }
        }

        private async Task CleanupTokensAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            // Single transaction for the entire cleanup process
            using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 1. Process tokens that need to be blacklisted (expired but not too old)
                await ProcessInBatchesAsync(
                    queryFactory: () => dbContext.SessionToken
                        .Where(s => s.RefreshTokenExpiration < now && // Expired
                                  s.RefreshTokenExpiration >= now.AddDays(-_deleteOlderThanDays) && // But not too old
                                  !s.IsRevoked) // Not already revoked
                        .AsQueryable(),
                    processBatch: async batch =>
                    {
                        foreach (var token in batch)
                        {
                            // Create blacklist entry
                            var blacklistEntry = new TokenBlacklist
                            {
                                SessionID = token.SessionID,
                                RefreshToken = token.RefreshToken,
                                BlacklistedAt = now,
                                Reason = "Expired token cleanup",
                                IPAddress = token.IPAddress
                            };

                            // Mark token as revoked
                            token.IsRevoked = true;
                            token.BlacklistedTokens = token.BlacklistedTokens ?? new List<TokenBlacklist>();
                            token.BlacklistedTokens.Add(blacklistEntry);
                        }
                        await dbContext.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Processed batch of {Count} tokens to blacklist", batch.Count);
                    },
                    itemDescription: "tokens to blacklist",
                    cancellationToken: cancellationToken);

                // 2. Process very old expired tokens (delete directly)
                await ProcessInBatchesAsync(
                    queryFactory: () => dbContext.SessionToken
                        .Where(s => s.RefreshTokenExpiration < now.AddDays(-_deleteOlderThanDays))
                        .AsQueryable(),
                    processBatch: async batch =>
                    {
                        dbContext.SessionToken.RemoveRange(batch);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Processed batch of {Count} very old expired tokens", batch.Count);
                    },
                    itemDescription: "very old expired tokens to delete",
                    cancellationToken: cancellationToken);

                // 3. Clean up old blacklist entries (older than _settings.BlacklistRetentionDays days)
                await ProcessInBatchesAsync(
                    queryFactory: () => dbContext.TokenBlacklist
                        .Where(b => b.BlacklistedAt < now.AddDays(-_deleteOlderThanDays))
                        .AsQueryable(),
                    processBatch: async batch =>
                    {
                        dbContext.TokenBlacklist.RemoveRange(batch);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Processed batch of {Count} old blacklist entries", batch.Count);
                    },
                    itemDescription: "old blacklist entries to delete",
                    cancellationToken: cancellationToken);

                // Commit the transaction if we got here without exceptions
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Successfully completed token cleanup at {UtcNow}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error rolling back transaction during cleanup error");
                }
                throw;
            }
        }

        private async Task ProcessInBatchesAsync<T>(
            Func<IQueryable<T>> queryFactory,
            Func<List<T>, Task> processBatch,
            string itemDescription,
            CancellationToken cancellationToken) where T : class
        {
            int totalProcessed = 0;
            int batchNumber = 0;
            const int maxIterations = 1000; // Safety limit to prevent infinite loops

            while (batchNumber < maxIterations && !cancellationToken.IsCancellationRequested)
            {
                // Get a fresh query and fetch a batch of items
                var batch = await queryFactory()
                    .Take(BatchSize)
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                {
                    break; // No more items to process
                }

                // Process the current batch
                try
                {
                    await processBatch(batch);
                    totalProcessed += batch.Count;
                    _logger.LogInformation("Processed batch of {Count} {ItemDescription}", 
                        batch.Count, itemDescription);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing batch of {ItemDescription}", itemDescription);
                    throw; // Re-throw to be handled by the caller
                }

                // Small delay to prevent high CPU usage and allow cancellation
                await Task.Delay(100, cancellationToken);
                batchNumber++;
            }

            if (totalProcessed > 0)
            {
                _logger.LogInformation("Successfully processed {Count} {ItemDescription}", 
                    totalProcessed, itemDescription);
            }
            else
            {
                _logger.LogDebug("No {ItemDescription} to process", itemDescription);
            }
        }
    }

    // Token cleanup default values if not specified in appsettings.json
    public class TokenCleanupSettings
    {
        public int CleanupIntervalDays { get; set; } = 1;
        public int DeleteOlderThanDays { get; set; } = 90;
    }
}
