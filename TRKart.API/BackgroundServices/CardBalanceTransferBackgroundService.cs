using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TRKart.Business.Interfaces;

namespace TRKart.API.BackgroundServices
{
    public class CardBalanceTransferBackgroundService : BackgroundService
    {
        private readonly ILogger<CardBalanceTransferBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromDays(1); // Run once a day
        // Delay before first run on startup. It runs third among the background services. 
        // After token cleanup and card expiration/blacklisting.
        private readonly TimeSpan _startupDelay = TimeSpan.FromMinutes(3); 

        public CardBalanceTransferBackgroundService(
            ILogger<CardBalanceTransferBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Card Balance Transfer Background Service is starting.");

            // Initial delay to allow the application to start up
            await Task.Delay(_startupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting scheduled balance transfer from blacklisted cards at {UtcNow}", DateTime.UtcNow);
                    
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var cardBalanceTransferService = scope.ServiceProvider.GetRequiredService<ICardBalanceTransferService>();
                        var transfersCompleted = await cardBalanceTransferService.TransferBalancesFromBlacklistedCardsAsync();
                        _logger.LogInformation("Completed balance transfer job. Transfers completed: {Count}", transfersCompleted);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing balance transfer job");
                }

                // Wait for the next interval
                try
                {
                    _logger.LogInformation("Next balance transfer check in {Interval} days.", _checkInterval);
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Exit gracefully if the service is being stopped
                    break;
                }
            }

            _logger.LogInformation("Card Balance Transfer Background Service is stopping.");
        }
    }
}
