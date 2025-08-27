using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Security.Claims;
using TRKart.Business.Interfaces;
using TRKart.Core.Helpers;
using TRKart.DataAccess;
using TRKart.Entities;
using TRKart.Entities.Models;
using TRKart.Entities.DTOs;
using TRKart.Core.Interfaces;

namespace TRKart.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtHelper _jwtHelper;

        private readonly IUniqueNumberChecker _uniqueNumberChecker;

        public AuthService(ApplicationDbContext context, JwtHelper jwtHelper, IUniqueNumberChecker uniqueNumberChecker)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _jwtHelper = jwtHelper ?? throw new ArgumentNullException(nameof(jwtHelper));
            _uniqueNumberChecker = uniqueNumberChecker ?? throw new ArgumentNullException(nameof(uniqueNumberChecker));
        }

        public async Task<bool> VerifyPasswordAsync(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return false;

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == email);

            if (customer == null)
                return false;

            return BCrypt.Net.BCrypt.Verify(password, customer.PasswordHash);
        }

        public async Task<TokenResponse?> LoginAsync(LoginDto dto, string? ipAddress = null, string? deviceInfo = null)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (customer == null)
                return null;

            bool isValid = await VerifyPasswordAsync(dto.Email, dto.Password);
            if (!isValid)
                return null;


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

            // Generate a new access token
            string newAccessToken = _jwtHelper.GenerateAccessToken(customer.Email, customer.CustomerID);
            DateTime accessTokenExpiration = _jwtHelper.GetAccessTokenExpiration();
            
            // Only rotate refresh token if it's close to expiration (e.g., within 1 day)
            bool shouldRotateRefreshToken = session.RefreshTokenExpiration < DateTime.UtcNow.AddDays(1);
            
            string newRefreshToken = shouldRotateRefreshToken 
                ? _jwtHelper.GenerateRefreshToken()
                : refreshToken;
                
            DateTime refreshTokenExpiration = shouldRotateRefreshToken 
                ? _jwtHelper.GetRefreshTokenExpiration()
                : session.RefreshTokenExpiration;

            if (shouldRotateRefreshToken)
            {
                // Only blacklist the old refresh token if we're rotating to a new one
                await BlacklistRefreshTokenAsync(refreshToken, "Refresh token rotation");
            }

            // Update the session with new access token and expiration
            // Keep the same refresh token if not rotating
            session.AccessToken = newAccessToken;
            session.AccessTokenExpiration = accessTokenExpiration;
            
            // Only update refresh token if we're rotating it
            if (shouldRotateRefreshToken)
            {
                session.RefreshToken = newRefreshToken;
                session.RefreshTokenExpiration = refreshTokenExpiration;
            }
            
            // Update IP address if provided
            if (!string.IsNullOrEmpty(ipAddress))
            {
                session.IPAddress = ipAddress;
            }

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

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out _))
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

            // Generate unique customer number
            var customerNumber = await CustomerNumberHelper.GenerateCustomerNumberAsync(_uniqueNumberChecker);

            // Create new user
            var newCustomer = new Customers
            {
                Email = dto.Email,
                FullName = dto.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CustomerNumber = customerNumber
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
                return principal?.FindFirst(ClaimTypes.Email)?.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool IsValid, string? Email, int? CustomerID, string? FullName)> ValidateRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return (false, null, null, null);

            // Check if token is blacklisted
            var isBlacklisted = await IsRefreshTokenBlacklistedAsync(refreshToken);
            if (isBlacklisted)
                return (false, null, null, null);

            // Get the token from database
            var token = await _context.SessionToken
                .Include(rt => rt.Customer)
                .FirstOrDefaultAsync(rt => rt.RefreshToken == refreshToken && 
                                       rt.RefreshTokenExpiration > DateTime.UtcNow && 
                                       !rt.IsRevoked);

            if (token == null || token.Customer == null)
                return (false, null, null, null);

            return (true, token.Customer.Email, token.Customer.CustomerID, token.Customer.FullName);
        }
        
        public async Task<bool> ChangePasswordAsync(ChangePasswordDto dto)
        {
            try
            {
                IDbContextTransaction? transaction = null;
                var isInMemory = _context.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true;
                if (!isInMemory)
                {
                    transaction = await _context.Database.BeginTransactionAsync();
                }
        
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == dto.Email);

                if (customer == null)
                    return false;

                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, customer.PasswordHash))
                    return false;

                // Reject if the new password matches the current password
                if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, customer.PasswordHash))
                    return false;

                var recentPasswords = await _context.PasswordHistory
                    .Where(ph => ph.CustomerID == customer.CustomerID)
                    .OrderByDescending(ph => ph.CreatedAt)
                    .Take(3)
                    .Select(ph => ph.PasswordHash)
                    .ToListAsync();

                if (recentPasswords.Any(oldHash => BCrypt.Net.BCrypt.Verify(dto.NewPassword, oldHash)))
                    return false;

                await _context.PasswordHistory.AddAsync(new PasswordHistory
                {
                    CustomerID = customer.CustomerID,
                    PasswordHash = customer.PasswordHash,
                    CreatedAt = DateTime.UtcNow
                });

                customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        
                await _context.SaveChangesAsync();
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ChangeEmailAsync(string currentEmail, ChangeEmailDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(currentEmail))
                    return false;

                if (dto == null || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.NewEmail))
                    return false;

                // Basic email validation: must contain '@' and '.'
                if (!dto.NewEmail.Contains('@') || !dto.NewEmail.Contains('.'))
                    return false;

                IDbContextTransaction? transaction = null;
                var isInMemory = _context.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true;
                if (!isInMemory)
                {
                    transaction = await _context.Database.BeginTransactionAsync();
                }

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == currentEmail);

                if (customer == null)
                    return false;

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash))
                    return false;

                // No-op if same email (case-insensitive)
                if (string.Equals(customer.Email, dto.NewEmail, StringComparison.OrdinalIgnoreCase))
                    return true;

                // Ensure new email is unique
                var emailExists = await _context.Customers.AnyAsync(x => x.Email == dto.NewEmail);
                if (emailExists)
                    return false;

                customer.Email = dto.NewEmail;
                await _context.SaveChangesAsync();
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}