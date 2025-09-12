using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TRKart.DataAccess;
using TRKart.Entities.Models;

namespace TRKart.API.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly IServiceProvider _serviceProvider;

        public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RateLimitingMiddleware> logger, IServiceProvider serviceProvider)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Rate limiting for authentication endpoints (but exclude check-session)
            if (context.Request.Path.StartsWithSegments("/api/auth") && 
                !context.Request.Path.Value.Contains("check-session"))
            {
                var clientIP = context.Connection.RemoteIpAddress?.ToString();
                var endpoint = context.Request.Path.Value;
                
                if (!string.IsNullOrEmpty(clientIP))
                {
                    var cacheKey = $"rate_limit_{clientIP}_{endpoint}";
                    
                    var attempts = _cache.GetOrCreate(cacheKey, entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                        return 0;
                    });

                    // Different limits for different endpoints (reduced for testing)
                    var maxAttempts = endpoint switch
                    {
                        "/api/auth/login" => 5,       // 5 login attempts per 15 minutes (reduced for testing)
                        "/api/auth/register" => 3,    // 3 registration attempts per 15 minutes (reduced for testing)
                        _ => 5                        // 5 attempts for other auth endpoints (reduced for testing)
                    };

                    // Always increment attempt count (even for blocked IPs)
                    _cache.Set(cacheKey, attempts + 1, TimeSpan.FromMinutes(15));

                    // Check if this IP is already blocked in the database
                    var isBlocked = await CheckIfIPIsBlockedAsync(clientIP, endpoint);
                    if (isBlocked)
                    {
                        _logger.LogWarning("Blocked IP {IP} attempted to access {Endpoint} (attempt #{Attempts})", clientIP, endpoint, attempts + 1);
                        
                        // Log the blocked attempt to increase RequestCount
                        await LogBlockedIPAttemptAsync(clientIP, endpoint, attempts + 1, context);
                        
                        context.Response.StatusCode = 429; // Too Many Requests
                        await context.Response.WriteAsync("Your IP is temporarily blocked due to excessive requests. Please try again later.");
                        return;
                    }

                    if (attempts >= maxAttempts)
                    {
                        _logger.LogWarning("Rate limit exceeded for IP: {IP} on endpoint: {Endpoint}", clientIP, endpoint);
                        
                        // Log rate limit violation to database
                        await LogRateLimitViolationAsync(clientIP, endpoint, attempts, maxAttempts, context);
                        
                        context.Response.StatusCode = 429; // Too Many Requests
                        await context.Response.WriteAsync("Too many requests. Please try again later.");
                        return;
                    }
                    
                    // Log every attempt for debugging
                    _logger.LogDebug("Rate limit attempt {Attempts}/{MaxAttempts} for IP: {IP} on endpoint: {Endpoint}", 
                        attempts + 1, maxAttempts, clientIP, endpoint);
                }
            }

            await _next(context);
        }

        private async Task<bool> CheckIfIPIsBlockedAsync(string clientIP, string endpoint)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var blockedEntry = await context.RateLimiting
                    .FirstOrDefaultAsync(r => r.Identifier == clientIP && 
                                            r.Endpoint == endpoint && 
                                            r.IsBlocked && 
                                            r.BlockedUntil > DateTimeOffset.UtcNow);

                return blockedEntry != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if IP {IP} is blocked", clientIP);
                return false; // If we can't check, allow the request
            }
        }

        private async Task LogBlockedIPAttemptAsync(string clientIP, string endpoint, int attemptNumber, HttpContext httpContext)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Find existing rate limit entry for this IP/endpoint
                var existingEntry = await context.RateLimiting
                    .FirstOrDefaultAsync(r => r.Identifier == clientIP && 
                                            r.Endpoint == endpoint);

                if (existingEntry != null)
                {
                    // Check if the existing entry is still blocked
                    if (existingEntry.IsBlocked && existingEntry.BlockedUntil > DateTimeOffset.UtcNow)
                    {
                        // Still blocked - update request count
                        existingEntry.RequestCount = attemptNumber;
                        existingEntry.LastRequestAt = DateTimeOffset.UtcNow;
                        
                        // Update the reason to show this was a blocked attempt
                        existingEntry.BlockReason = $"{existingEntry.BlockReason} | Blocked attempt #{attemptNumber} at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}";
                        
                        await context.SaveChangesAsync();
                        
                        _logger.LogDebug("Updated RequestCount to {RequestCount} for blocked IP {IP} on endpoint {Endpoint}", 
                            attemptNumber, clientIP, endpoint);
                    }
                    else
                    {
                        // Block has expired - create new entry for new cycle
                        await CreateNewRateLimitEntryAsync(httpContext, clientIP, endpoint, attemptNumber);
                    }
                }
                else
                {
                    // No entry found - create new entry for new rate limit cycle
                    await CreateNewRateLimitEntryAsync(httpContext, clientIP, endpoint, attemptNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log blocked IP attempt for IP: {IP} on endpoint: {Endpoint}", clientIP, endpoint);
            }
        }

        private async Task CreateNewRateLimitEntryAsync(HttpContext httpContext, string clientIP, string endpoint, int attemptNumber)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Get customer ID from request body if applicable
                var customerId = await GetCustomerIdFromRequestAsync(context, endpoint, httpContext);
                
                // Create new entry for a new rate limit cycle
                var newEntry = new RateLimiting
                {
                    Identifier = clientIP,
                    IdentifierType = "IP",
                    Endpoint = endpoint,
                    CustomerID = customerId,
                    RequestCount = attemptNumber,
                    FirstRequestAt = DateTimeOffset.UtcNow,
                    LastRequestAt = DateTimeOffset.UtcNow,
                    IsBlocked = false, // Not blocked yet
                    BlockedUntil = null,
                    BlockReason = $"New rate limit cycle started. Attempt #{attemptNumber}",
                    ViolationCount = 0
                };

                context.RateLimiting.Add(newEntry);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Created new rate limit entry for IP: {IP} on endpoint: {Endpoint} after previous block expired. CustomerID: {CustomerID}", 
                    clientIP, endpoint, customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create new rate limit entry for IP: {IP} on endpoint: {Endpoint}", clientIP, endpoint);
            }
        }

        private async Task LogRateLimitViolationAsync(string clientIP, string endpoint, int attempts, int maxAttempts, HttpContext httpContext)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Get customer ID from request body if applicable
                var customerId = await GetCustomerIdFromRequestAsync(context, endpoint, httpContext);

                // Check if we already have a rate limit entry for this IP/endpoint combination
                var existingEntry = await context.RateLimiting
                    .FirstOrDefaultAsync(r => r.Identifier == clientIP && 
                                            r.Endpoint == endpoint);

                if (existingEntry != null)
                {
                    // Update existing entry - increase request count and extend block if needed
                    existingEntry.RequestCount = attempts; // Use current attempt count, not cumulative
                    existingEntry.ViolationCount = (existingEntry.ViolationCount ?? 0) + 1;
                    
                    // Update customer ID if we found one
                    if (customerId.HasValue)
                    {
                        existingEntry.CustomerID = customerId.Value;
                    }
                    
                    // If still within the original block period, keep the original BlockedUntil
                    // If block period has expired, start a new block period
                    if (existingEntry.BlockedUntil <= DateTimeOffset.UtcNow)
                    {
                        // Calculate escalating block duration based on violation count
                        var blockMinutes = Math.Min(15 * existingEntry.ViolationCount.Value, 1440); // Max 24 hours
                        existingEntry.BlockedUntil = DateTimeOffset.UtcNow.AddMinutes(blockMinutes);
                        existingEntry.BlockReason = $"Rate limit exceeded: {attempts}/{maxAttempts} attempts. Violation #{existingEntry.ViolationCount}. Blocked for {blockMinutes} minutes.";
                    }
                    else
                    {
                        // Still blocked, just update the reason
                        existingEntry.BlockReason = $"Rate limit exceeded: {attempts}/{maxAttempts} attempts. Violation #{existingEntry.ViolationCount}. Blocked until {existingEntry.BlockedUntil:yyyy-MM-dd HH:mm:ss}.";
                    }
                    
                    existingEntry.LastViolationAt = DateTimeOffset.UtcNow;
                    existingEntry.IsBlocked = true;
                }
                else
                {
                    // Create new entry
                    var rateLimitEntry = new RateLimiting
                    {
                        Identifier = clientIP,
                        IdentifierType = "IP",
                        Endpoint = endpoint,
                        CustomerID = customerId, // Use the customer ID we found, or null if not found
                        RequestCount = attempts,
                        ViolationCount = 1,
                        IsBlocked = true,
                        BlockedUntil = DateTimeOffset.UtcNow.AddMinutes(15),
                        BlockReason = $"Rate limit exceeded: {attempts}/{maxAttempts} attempts. First violation.",
                        FirstViolationAt = DateTimeOffset.UtcNow,
                        LastViolationAt = DateTimeOffset.UtcNow
                    };

                    context.RateLimiting.Add(rateLimitEntry);
                }

                await context.SaveChangesAsync();
                
                // If we have a CustomerID, also update the Customers table AccountLockedUntil field
                if (customerId.HasValue)
                {
                    var customer = await context.Customers.FindAsync(customerId.Value);
                    if (customer != null)
                    {
                        var blockedUntil = existingEntry?.BlockedUntil ?? DateTimeOffset.UtcNow.AddMinutes(15);
                        customer.AccountLockedUntil = blockedUntil;
                        await context.SaveChangesAsync();
                        
                        _logger.LogWarning("Customer {CustomerID} account locked until {BlockedUntil} due to rate limiting", 
                            customerId.Value, blockedUntil);
                    }
                }
                
                _logger.LogWarning("Rate limit violation logged for IP: {IP} on endpoint: {Endpoint}. Total attempts: {TotalAttempts}, Violations: {Violations}, Blocked until: {BlockedUntil}, CustomerID: {CustomerID}", 
                    clientIP, endpoint, existingEntry?.RequestCount ?? attempts, existingEntry?.ViolationCount ?? 1, existingEntry?.BlockedUntil ?? DateTimeOffset.UtcNow.AddMinutes(15), customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log rate limit violation for IP: {IP} on endpoint: {Endpoint}", clientIP, endpoint);
            }
        }

        private async Task<int?> GetCustomerIdFromRequestAsync(ApplicationDbContext context, string endpoint, HttpContext httpContext)
        {
            try
            {
                // Only try to extract customer ID for auth endpoints
                if (!endpoint.Contains("/api/auth/login") && !endpoint.Contains("/api/auth/register"))
                {
                    return null;
                }

                // Read the request body to get email
                var requestBody = await ReadRequestBodyAsync(httpContext);
                if (string.IsNullOrEmpty(requestBody))
                {
                    return null;
                }

                var email = ExtractEmailFromRequestBody(requestBody);
                if (string.IsNullOrEmpty(email))
                {
                    return null;
                }

                // Find customer by email
                var customer = await context.Customers.FirstOrDefaultAsync(c => c.Email == email);
                return customer?.CustomerID;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not extract customer ID from request body for endpoint: {Endpoint}", endpoint);
                return null;
            }
        }

        private async Task<string> ReadRequestBodyAsync(HttpContext httpContext)
        {
            try
            {
                // Enable buffering to allow reading the request body multiple times
                httpContext.Request.EnableBuffering();
                httpContext.Request.Body.Position = 0;
                
                using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                
                // Reset position for the next middleware
                httpContext.Request.Body.Position = 0;
                
                return body;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string ExtractEmailFromRequestBody(string requestBody)
        {
            try
            {
                // Simple JSON parsing to extract email field
                // This is a basic implementation - in production you might want to use a proper JSON parser
                var emailMatch = System.Text.RegularExpressions.Regex.Match(requestBody, @"""email""\s*:\s*""([^""]+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (emailMatch.Success)
                {
                    return emailMatch.Groups[1].Value;
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}
