using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TRKart.Business.Interfaces;
using TRKart.Core.Helpers;
using TRKart.DataAccess;
using TRKart.Entities;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;

namespace TRKart.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtHelper _jwtHelper;

        public AuthService(ApplicationDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
        }

        public async Task<TokenResponse?> LoginAsync(LoginDto dto, string? ipAddress = null, string? deviceInfo = null)
{
    var customer = await _context.Customers
        .FirstOrDefaultAsync(x => x.Email == dto.Email);

    if (customer == null)
        return null;

    bool isValid = BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash);
    if (!isValid)
        return null;

    // Check for existing valid refresh token for this user
    var existingSession = await _context.SessionToken
        .FirstOrDefaultAsync(s => s.CustomerID == customer.CustomerID &&
                                s.RefreshTokenExpiration > DateTime.UtcNow &&
                                !s.IsRevoked);

    if (existingSession != null)
    {
        // IMPORTANT: Check if the existing refresh token is blacklisted
        bool isBlacklisted = await IsRefreshTokenBlacklistedAsync(existingSession.RefreshToken);

        if (isBlacklisted)
        {
            // Mark the session as revoked since its refresh token is blacklisted
            existingSession.IsRevoked = true;
            await _context.SaveChangesAsync();

            // Continue to create new tokens instead of using the blacklisted one
        }
        else
        {
            // We have a valid existing session, just generate a new access token
            string newAccessToken = _jwtHelper.GenerateAccessToken(customer.Email, customer.CustomerID);
            DateTime newAccessTokenExpiration = _jwtHelper.GetAccessTokenExpiration();

            // Update the existing session with new access token
            existingSession.AccessToken = newAccessToken;
            existingSession.AccessTokenExpiration = newAccessTokenExpiration;
            existingSession.IPAddress = ipAddress ?? existingSession.IPAddress;
            existingSession.DeviceInfo = deviceInfo ?? existingSession.DeviceInfo;

            await _context.SaveChangesAsync();

            return new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = existingSession.RefreshToken, // Keep the existing refresh token
                AccessTokenExpiration = newAccessTokenExpiration,
                RefreshTokenExpiration = existingSession.RefreshTokenExpiration
            };
        }
    }

    // No valid existing session found OR existing session had blacklisted token, create new tokens
    string accessToken = _jwtHelper.GenerateAccessToken(customer.Email, customer.CustomerID);
    string refreshToken = _jwtHelper.GenerateRefreshToken();

    // Get token expiration times
    DateTime accessTokenExpiration = _jwtHelper.GetAccessTokenExpiration();
    DateTime refreshTokenExpiration = _jwtHelper.GetRefreshTokenExpiration();

    // Create and save new session
    var session = new SessionToken
    {
        CustomerID = customer.CustomerID,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        AccessTokenExpiration = accessTokenExpiration,
        RefreshTokenExpiration = refreshTokenExpiration,
        RefreshTokenCreatedAt = DateTime.UtcNow,
        IsRevoked = false,
        DeviceInfo = deviceInfo,
        IPAddress = ipAddress
    };

    await _context.SessionToken.AddAsync(session);
    await _context.SaveChangesAsync();

    return new TokenResponse
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        AccessTokenExpiration = accessTokenExpiration,
        RefreshTokenExpiration = refreshTokenExpiration
    };
}

        public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
        {
            // First check if the token is blacklisted
            if (await IsRefreshTokenBlacklistedAsync(refreshToken))
                return null;

            // Find the session with the provided refresh token
            var session = await _context.SessionToken
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && 
                                       s.RefreshTokenExpiration > DateTime.UtcNow && 
                                       !s.IsRevoked);

            if (session == null)
                return null;

            var customer = session.Customer;
            if (customer == null)
                return null;

            // Check for suspicious activity (IP address change without reason)
            bool suspiciousActivity = !string.IsNullOrEmpty(session.IPAddress) && 
                                     !string.IsNullOrEmpty(ipAddress) && 
                                     session.IPAddress != ipAddress;

            if (suspiciousActivity)
            {
                // For financial applications, we should be extra cautious
                // Log this suspicious activity
                Console.WriteLine($"Suspicious refresh token usage detected! Previous IP: {session.IPAddress}, Current IP: {ipAddress}");

                // Add the old token to blacklist
                await BlacklistRefreshTokenAsync(refreshToken, $"Suspicious IP change: {session.IPAddress} -> {ipAddress}");

                // Mark the token as revoked
                session.IsRevoked = true;
                await _context.SaveChangesAsync();

                // For financial applications, we might want to trigger additional security measures here
                // such as requiring re-authentication or notifying the user

                return null; // Don't allow refresh from suspicious activity
            }

            // Generate a new access token and refresh token
            string newAccessToken = _jwtHelper.GenerateAccessToken(customer.Email, customer.CustomerID);
            string newRefreshToken = _jwtHelper.GenerateRefreshToken();

            // Get token expiration times
            DateTime accessTokenExpiration = _jwtHelper.GetAccessTokenExpiration();
            DateTime refreshTokenExpiration = _jwtHelper.GetRefreshTokenExpiration();

            // Add the old refresh token to blacklist to prevent reuse (refresh token rotation)
            await BlacklistRefreshTokenAsync(refreshToken, "Refresh token rotation");

            // Update the session with new tokens
            session.AccessToken = newAccessToken;
            session.RefreshToken = newRefreshToken;
            session.AccessTokenExpiration = accessTokenExpiration;
            session.RefreshTokenExpiration = refreshTokenExpiration;
            session.IPAddress = ipAddress ?? session.IPAddress;

            await _context.SaveChangesAsync();

            return new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiration = accessTokenExpiration,
                RefreshTokenExpiration = refreshTokenExpiration
            };
        }

        public async Task<(bool IsValid, string? Email, int? CustomerID, string? FullName)> ValidateAccessTokenAsync(string accessToken)
        {
            try
            {
                // First, try to validate the token using JWT validation
                var principal = _jwtHelper.GetPrincipalFromExpiredToken(accessToken);
                var email = principal.FindFirstValue(ClaimTypes.Email);
                var customerIdClaim = principal.FindFirstValue("CustomerId");

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
                    return (false, null, null, null);

                // Then check if the token exists in database and is not expired or revoked
                var session = await _context.SessionToken
                    .Include(s => s.Customer)
                    .FirstOrDefaultAsync(s => s.AccessToken == accessToken &&
                                              s.AccessTokenExpiration > DateTime.UtcNow &&
                                              !s.IsRevoked);

                if (session == null)
                    return (false, null, null, null);

                // IMPORTANT: Check if the session's refresh token is blacklisted
                bool isRefreshTokenBlacklisted = await IsRefreshTokenBlacklistedAsync(session.RefreshToken);

                if (isRefreshTokenBlacklisted)
                {
                    // Mark the session as revoked since its refresh token is blacklisted
                    session.IsRevoked = true;
                    await _context.SaveChangesAsync();
                    return (false, null, null, null);
                }

                return (true, session.Customer.Email, session.Customer.CustomerID, session.Customer.FullName);
            }
            catch
            {
                return (false, null, null, null);
            }
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            try
            {
                var session = await _context.SessionToken
                    .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

                if (session == null)
                    return false;

                // Mark the token as revoked
                session.IsRevoked = true;

                // Add to blacklist with reason
                await BlacklistRefreshTokenAsync(refreshToken, "User-initiated logout");

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RevokeToken] Error: {ex}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[RevokeToken] Inner Exception: {ex.InnerException}");

                return false;
            }
        }

        public async Task<bool> IsRefreshTokenBlacklistedAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return true; // Consider empty tokens as blacklisted for security

            return await _context.TokenBlacklist
                .AnyAsync(t => t.RefreshToken == refreshToken);
        }

        public async Task BlacklistRefreshTokenAsync(string refreshToken, string reason)
        {
            // Check if already blacklisted
            if (await IsRefreshTokenBlacklistedAsync(refreshToken))
                return;

            // Add to blacklist
            var blacklistEntry = new TokenBlacklist
            {
                RefreshToken = refreshToken,
                BlacklistedAt = DateTime.UtcNow,
                Reason = reason
            };

            await _context.TokenBlacklist.AddAsync(blacklistEntry);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> RegisterAsync(RegisterDto dto)
        {
            // Check if a user with the same email already exists
            var exists = await _context.Customers.AnyAsync(x => x.Email == dto.Email);
            if (exists)
                return false;

            // Create new user
            var newCustomer = new Customers
            {
                Email = dto.Email,
                FullName = dto.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            await _context.Customers.AddAsync(newCustomer);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<int?> GetCustomerIdFromAccessTokenAsync(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
                return null;

            try
            {
                var principal = _jwtHelper.GetPrincipalFromExpiredToken(accessToken);
                var customerIdClaim = principal.FindFirstValue("CustomerId");

                if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
                    return null;

                // Verify token exists in the database and is not revoked
                var session = await _context.SessionToken
                    .FirstOrDefaultAsync(s => s.AccessToken == accessToken && !s.IsRevoked);

                if (session == null)
                    return null;

                return customerId;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> GetUserEmailByAccessTokenAsync(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
                return null;

            try
            {
                var principal = _jwtHelper.GetPrincipalFromExpiredToken(accessToken);
                var email = principal.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(email))
                    return null;

                // Verify token exists in the database and is not revoked
                var session = await _context.SessionToken
                    .Include(s => s.Customer)
                    .FirstOrDefaultAsync(s => s.AccessToken == accessToken && !s.IsRevoked);

                if (session == null)
                    return null;

                return email;
            }
            catch
            {
                return null;
            }
        }
    }
}