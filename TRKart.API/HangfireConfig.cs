using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TRKart.API.BackgroundServices.Hangfire;
using TRKart.Business.Interfaces;
using TRKart.Business.Services;
using TRKart.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace TRKart.API
{
    public static class HangfireConfig
    {
        /// <summary>
        /// Adds Hangfire services to the specified IServiceCollection.
        /// </summary>
        public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Hangfire services with dedicated connection string
            var hangfireConnectionString = configuration.GetConnectionString("HangfireConnection") ?? 
                throw new InvalidOperationException("HangfireConnection string is not configured");
                
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(hangfireConnectionString));

            // Add the processing server as IHostedService
            services.AddHangfireServer(options =>
            {
                options.ServerName = $"{Environment.MachineName}";
                options.WorkerCount = 1; // Adjust based on your needs
            });

            // Register Hangfire services
            services.AddScoped<HangfireCardBalanceTransferService>();
            services.AddScoped<HangfireCardExpirationService>();

            // Register services needed by Hangfire jobs
            services.AddScoped<ICardBalanceTransferService, CardBalanceTransferService>();
            services.AddScoped<ICardExpirationService, CardExpirationService>();
            
            // Register DbContext if not already registered
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            return services;
        }

        /// <summary>
        /// Configures the Hangfire dashboard with authentication.
        /// </summary>
        public static IApplicationBuilder UseHangfireDashboardWithAuth(this IApplicationBuilder app, IConfiguration configuration)
        {
            var dashboardOptions = new DashboardOptions
            {
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
                DashboardTitle = "TRKart Background Jobs",
                StatsPollingInterval = 60000, // 1 minute
                DisplayStorageConnectionString = false,
                IgnoreAntiforgeryToken = true
            };

            app.UseHangfireDashboard("/hangfire", dashboardOptions);

            // Schedule recurring jobs
            RecurringJob.AddOrUpdate<HangfireCardBalanceTransferService>(
                "card-balance-transfer",
                x => x.TransferBalancesAsync(),
                Cron.Daily, // Runs once a day at midnight
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            RecurringJob.AddOrUpdate<HangfireCardExpirationService>(
                "card-expiration-check",
                x => x.CheckAndUpdateExpiredCardsAsync(),
                Cron.Monthly, // Runs on the 1st day of every month at midnight
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            return app;
        }
    }

    // Custom authorization filter for Hangfire Dashboard
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return AuthorizeStatic(context);
        }

        private static bool AuthorizeStatic(DashboardContext context)
        {
            if (context == null) return false;
            
            var httpContext = context.GetHttpContext();
            if (httpContext?.User?.Identity == null) return false;
            
            // In a real application, implement proper authorization here
            // For example, check if the current user is an admin
            return httpContext.User.Identity.IsAuthenticated && 
                   httpContext.User.IsInRole("Admin");
        }
    }
}
