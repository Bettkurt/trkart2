using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TRKart.DataAccess;
using TRKart.Entities.Models;

namespace TRKart.API.BackgroundServices
{
    public class RateLimitCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RateLimitCleanupBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

        public RateLimitCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<RateLimitCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Rate Limit Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredBlocksAsync();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during rate limit cleanup");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait 1 minute on error
                }
            }

            _logger.LogInformation("Rate Limit Cleanup Service stopped");
        }

        private async Task CleanupExpiredBlocksAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var expiredBlocks = await context.RateLimiting
                    .Where(r => r.IsBlocked && r.BlockedUntil <= DateTimeOffset.UtcNow)
                    .ToListAsync();

                if (expiredBlocks.Any())
                {
                    var customerIdsToUnlock = new List<int>();
                    
                    foreach (var block in expiredBlocks)
                    {
                        // Mark block as expired
                        block.IsBlocked = false;
                        block.BlockReason = $"Block expired at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}. Previous reason: {block.BlockReason}";
                        
                        // If this block has a CustomerID, we need to clear their AccountLockedUntil
                        if (block.CustomerID.HasValue)
                        {
                            customerIdsToUnlock.Add(block.CustomerID.Value);
                        }
                        
                        _logger.LogInformation("Expired rate limit block for {Identifier} on {Endpoint}. Total requests: {RequestCount}, CustomerID: {CustomerID}", 
                            block.Identifier, block.Endpoint, block.RequestCount, block.CustomerID);
                    }

                    await context.SaveChangesAsync();
                    
                    // Clear AccountLockedUntil for customers whose rate limit blocks have expired
                    if (customerIdsToUnlock.Any())
                    {
                        var customersToUnlock = await context.Customers
                            .Where(c => customerIdsToUnlock.Contains(c.CustomerID))
                            .ToListAsync();
                            
                        foreach (var customer in customersToUnlock)
                        {
                            customer.AccountLockedUntil = null;
                            _logger.LogInformation("Cleared AccountLockedUntil for customer {CustomerID} due to expired rate limit", 
                                customer.CustomerID);
                        }
                        
                        await context.SaveChangesAsync();
                    }
                    
                    _logger.LogInformation("Cleaned up {Count} expired rate limit blocks and unlocked {CustomerCount} customer accounts", 
                        expiredBlocks.Count, customerIdsToUnlock.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired rate limit blocks");
            }
        }
    }
}

