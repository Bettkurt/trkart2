using Microsoft.EntityFrameworkCore;
using TRKart.DataAccess;

namespace TRKart.API.Services
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run once a day

        public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupTokensAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while cleaning up tokens.");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("Token Cleanup Service is stopping.");
        }

        private async Task CleanupTokensAsync()
        {
            _logger.LogInformation("Starting token cleanup task.");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.UtcNow;
            var monthAgo = now.AddMonths(-1);

            // Remove expired sessions that are older than 1 month
            var oldSessions = await dbContext.SessionToken
                .Where(s => s.RefreshTokenExpiration < now && s.CreatedAt < monthAgo)
                .ToListAsync();

            if (oldSessions.Any())
            {
                _logger.LogInformation($"Removing {oldSessions.Count} expired session tokens.");
                dbContext.SessionToken.RemoveRange(oldSessions);
            }

            // Clean up blacklist entries older than 3 months
            var oldBlacklistEntries = await dbContext.TokenBlacklist
                .Where(b => b.BlacklistedAt < now.AddMonths(-3))
                .ToListAsync();

            if (oldBlacklistEntries.Any())
            {
                _logger.LogInformation($"Removing {oldBlacklistEntries.Count} old blacklist entries.");
                dbContext.TokenBlacklist.RemoveRange(oldBlacklistEntries);
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Token cleanup completed.");
        }
    }
}
