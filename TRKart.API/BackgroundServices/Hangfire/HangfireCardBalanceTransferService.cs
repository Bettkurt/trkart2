using Hangfire;
using Microsoft.Extensions.Logging;
using TRKart.Business.Interfaces;

namespace TRKart.API.BackgroundServices.Hangfire
{
    public class HangfireCardBalanceTransferService
    {
        private readonly ILogger<HangfireCardBalanceTransferService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public HangfireCardBalanceTransferService(
            ILogger<HangfireCardBalanceTransferService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task TransferBalancesAsync()
        {
            _logger.LogInformation("Starting Hangfire balance transfer job at {UtcNow}", DateTime.UtcNow);
            
            using var scope = _serviceProvider.CreateScope();
            var cardBalanceTransferService = scope.ServiceProvider.GetRequiredService<ICardBalanceTransferService>();
            var transfersCompleted = await cardBalanceTransferService.TransferBalancesFromBlacklistedCardsAsync();
            
            _logger.LogInformation("Completed Hangfire balance transfer job. Transfers completed: {Count}", transfersCompleted);
        }
    }
}
