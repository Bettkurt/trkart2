using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TRKart.Business.Interfaces;

namespace TRKart.API.BackgroundServices
{
    public class CardExpirationBackgroundService : BackgroundService
    {
        private readonly ILogger<CardExpirationBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromDays(30); // Run once a month day // FromDays(30)
        private readonly TimeSpan _startupDelay = TimeSpan.FromMinutes(1); // Delay before first run on startup

        public CardExpirationBackgroundService(
            ILogger<CardExpirationBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Card Expiration Background Service is starting.");

            // Initial delay to allow the application to fully start
            await Task.Delay(_startupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting card expiration check at {UtcNow}", DateTime.UtcNow);

                    // Create a new scope for this operation
                    using var scope = _serviceProvider.CreateScope();
                    var cardExpirationService = scope.ServiceProvider.GetRequiredService<ICardExpirationService>();
                    
                    await cardExpirationService.CheckAndUpdateExpiredCardsAsync();

                    _logger.LogInformation("Completed card expiration check at {UtcNow}", DateTime.UtcNow);
                }
                catch (Exception ex) when (ex is not TaskCanceledException)
                {
                    _logger.LogError(ex, "Error in card expiration background service");
                }

                // Wait until next check interval or until service is stopped
                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Service is stopping
                    break;
                }
            }

            _logger.LogInformation("Card Expiration Background Service is stopping.");
        }
    }
}
