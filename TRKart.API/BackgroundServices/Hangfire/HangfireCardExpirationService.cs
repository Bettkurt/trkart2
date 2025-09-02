using Hangfire;
using Microsoft.Extensions.Logging;
using TRKart.Business.Interfaces;

namespace TRKart.API.BackgroundServices.Hangfire
{
    public class HangfireCardExpirationService
    {
        private readonly ILogger<HangfireCardExpirationService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public HangfireCardExpirationService(
            ILogger<HangfireCardExpirationService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task CheckAndUpdateExpiredCardsAsync()
        {
            _logger.LogInformation("Starting Hangfire card expiration check at {UtcNow}", DateTime.UtcNow);
            
            using var scope = _serviceProvider.CreateScope();
            var cardExpirationService = scope.ServiceProvider.GetRequiredService<ICardExpirationService>();
            
            await cardExpirationService.CheckAndUpdateExpiredCardsAsync();
            
            _logger.LogInformation("Completed Hangfire card expiration check at {UtcNow}", DateTime.UtcNow);
        }
    }
}
