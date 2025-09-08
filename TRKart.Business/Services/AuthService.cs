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
using Microsoft.Extensions.Logging;


namespace TRKart.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtHelper _jwtHelper;
        private readonly IUniqueNumberChecker _uniqueNumberChecker;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, JwtHelper jwtHelper, IUniqueNumberChecker uniqueNumberChecker, ILogger<AuthService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _jwtHelper = jwtHelper ?? throw new ArgumentNullException(nameof(jwtHelper));
            _uniqueNumberChecker = uniqueNumberChecker ?? throw new ArgumentNullException(nameof(uniqueNumberChecker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> VerifyPasswordAsync(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Password verification failed - missing credentials for email: {Email}", email);
                return false;
            }

            try
            {
                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Email == email);

                if (customer == null)
                {
                    _logger.LogWarning("Password verification failed - customer not found for email: {Email}", email);
                    return false;
                }

                bool isValid = BCrypt.Net.BCrypt.Verify(password, customer.PasswordHash);
                _logger.LogDebug("Password verification result for email {Email}: {IsValid}", email, isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password verification error for email: {Email}", email);
                return false;
            }
        }

        public async Task<TokenResponse?> LoginAsync(LoginDto dto, string? ipAddress = null, string? deviceInfo = null)
        {
            _logger.LogInformation("Login attempt for email: {Email} from IP: {IPAddress}", dto.Email, ipAddress);

            bool isValid = await VerifyPasswordAsync(dto.Email, dto.Password);
            if (!isValid)
            {
                return null;
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync( x => x.Email == dto.Email);

            // No null customer check is needed. VerifyPasswordAsync already checks if the customer exists.

            // No valid existing session found OR existing session had blacklisted token, create new tokens
            string accessToken = _jwtHelper.GenerateAccessToken(customer.Email, customer.CustomerID);
            string refreshToken = _jwtHelper.GenerateRefreshToken();

            // Get token expiration times
            DateTimeOffset accessTokenExpiration = _jwtHelper.GetAccessTokenExpiration();
            DateTimeOffset refreshTokenExpiration = _jwtHelper.GetRefreshTokenExpiration();

            // Create and save new session
            var session = new SessionToken
            {
                CustomerID = customer.CustomerID,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = accessTokenExpiration,
                RefreshTokenExpiration = refreshTokenExpiration,
                // RefreshTokenCreatedAt = DateTime.UtcNow, // Set by DB
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
            _logger.LogInformation("Refresh token attempt for token: {RefreshToken} from IP: {IPAddress}", refreshToken, ipAddress);

            // First check if the token is blacklisted
            if (await IsRefreshTokenBlacklistedAsync(refreshToken))
            {
                _logger.LogWarning("Refresh token blacklisted: {RefreshToken}", refreshToken);
                return null;
            }

            // Find the session with the provided refresh token
            var session = await _context.SessionToken
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && 
                                       s.RefreshTokenExpiration > DateTimeOffset.UtcNow && 
                                       !s.IsRevoked);

            if (session == null)
            {
                _logger.LogWarning("Refresh token not found in session: {RefreshToken}", refreshToken);
                return null;
            }

            var customer = session.Customer;
            if (customer == null)
            {
                _logger.LogWarning("Customer not found for refresh token session: {RefreshToken}", refreshToken);
                return null;
            }

            // Check for suspicious activity (IP address change without reason)
            bool suspiciousActivity = !string.IsNullOrEmpty(session.IPAddress) && 
                                     !string.IsNullOrEmpty(ipAddress) && 
                                     session.IPAddress != ipAddress;

            if (suspiciousActivity)
            {
                _logger.LogWarning("Suspicious refresh token usage detected! Previous IP: {PreviousIP}, Current IP: {CurrentIP}", session.IPAddress, ipAddress);

                // Add the old token to blacklist
                await BlacklistRefreshTokenAsync(refreshToken, $"Suspicious IP change: {session.IPAddress} -> {ipAddress}");

                // Mark the token as revoked
                session.IsRevoked = true;
                await _context.SaveChangesAsync();

                // TODO: We might want to trigger additional security measures here
                // such as requiring re-authentication or notifying the user

                _logger.LogWarning("Refresh token revoked due to suspicious activity for token: {RefreshToken}", refreshToken);
                return null; // Don't allow refresh from suspicious activity
            }

            // Generate a new access token
            string newAccessToken = _jwtHelper.GenerateAccessToken(customer.Email, customer.CustomerID);
            DateTimeOffset accessTokenExpiration = _jwtHelper.GetAccessTokenExpiration();
            
            // Only rotate refresh token if it's close to expiration (e.g., within 1 day)
            bool shouldRotateRefreshToken = session.RefreshTokenExpiration < DateTimeOffset.UtcNow.AddDays(1);
            
            string newRefreshToken = shouldRotateRefreshToken 
                ? _jwtHelper.GenerateRefreshToken()
                : refreshToken;
                
            DateTimeOffset refreshTokenExpiration = shouldRotateRefreshToken 
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

            _logger.LogInformation("Refresh token successful for token: {RefreshToken}", refreshToken);
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
            _logger.LogInformation("Validating access token: {AccessToken}", accessToken);
            try
            {
                // First, try to validate the token using JWT validation
                var principal = _jwtHelper.GetPrincipalFromExpiredToken(accessToken);
                var email = principal.FindFirstValue(ClaimTypes.Email);
                var customerIdClaim = principal.FindFirstValue("CustomerId");

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
                {
                    _logger.LogWarning("Invalid access token format for: {AccessToken}", accessToken);
                    return (false, null, null, null);
                }

                // Then check if the token exists in database and is not expired or revoked
                var session = await _context.SessionToken
                    .Include(s => s.Customer)
                    .FirstOrDefaultAsync(s => s.AccessToken == accessToken &&
                                              s.AccessTokenExpiration > DateTimeOffset.UtcNow &&
                                              !s.IsRevoked);

                if (session == null)
                {
                    _logger.LogWarning("Access token not found in session: {AccessToken}", accessToken);
                    return (false, null, null, null);
                }

                // IMPORTANT: Check if the session's refresh token is blacklisted
                bool isRefreshTokenBlacklisted = await IsRefreshTokenBlacklistedAsync(session.RefreshToken);

                if (isRefreshTokenBlacklisted)
                {
                    _logger.LogWarning("Access token refresh token blacklisted for session: {AccessToken}", accessToken);
                    // Mark the session as revoked since its refresh token is blacklisted
                    session.IsRevoked = true;
                    await _context.SaveChangesAsync();
                    return (false, null, null, null);
                }

                _logger.LogInformation("Access token validated successfully for customer: {CustomerID}", session.Customer.CustomerID);
                return (true, session.Customer.Email, session.Customer.CustomerID, session.Customer.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating access token: {AccessToken}", accessToken);
                return (false, null, null, null);
            }
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            _logger.LogInformation("Revoking token for refresh token: {RefreshToken}", refreshToken);
            try
            {
                var session = await _context.SessionToken
                    .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

                if (session == null)
                {
                    _logger.LogWarning("Refresh token not found for revocation: {RefreshToken}", refreshToken);
                    return false;
                }

                // Mark the token as revoked
                session.IsRevoked = true;

                // Add to blacklist with reason
                await BlacklistRefreshTokenAsync(refreshToken, "User-initiated logout");

                await _context.SaveChangesAsync();
                _logger.LogInformation("Token revoked successfully for refresh token: {RefreshToken}", refreshToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token for refresh token: {RefreshToken}", refreshToken);
                if (ex.InnerException != null)
                    _logger.LogError(ex.InnerException, "Inner exception for revoking token: {RefreshToken}", refreshToken);

                return false;
            }
        }

        public async Task<bool> IsRefreshTokenBlacklistedAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Checking blacklisted status for empty refresh token.");
                return true; // Consider empty tokens as blacklisted for security
            }

            bool isBlacklisted = await _context.TokenBlacklist
                .AnyAsync(t => t.RefreshToken == refreshToken);
            _logger.LogDebug("Refresh token {RefreshToken} is blacklisted: {IsBlacklisted}", refreshToken, isBlacklisted);
            return isBlacklisted;
        }

        public async Task BlacklistRefreshTokenAsync(string refreshToken, string reason)
        {
            _logger.LogInformation("Blacklisting refresh token: {RefreshToken} with reason: {Reason}", refreshToken, reason);
            // Check if already blacklisted
            if (await IsRefreshTokenBlacklistedAsync(refreshToken))
            {
                _logger.LogDebug("Refresh token {RefreshToken} already blacklisted.", refreshToken);
                return;
            }

            // Find the session with this refresh token
            var session = await _context.SessionToken
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

            if (session == null)
            {
                Console.WriteLine("Attempted to blacklist a refresh token with no associated session");
                return;
            }

            // Add to blacklist with SessionID
            var blacklistEntry = new TokenBlacklist
            {
                SessionID = session.SessionID,
                RefreshToken = refreshToken,
                IPAddress = session.IPAddress,
                BlacklistedAt = DateTimeOffset.UtcNow,
                Reason = reason
            };

            await _context.TokenBlacklist.AddAsync(blacklistEntry);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Refresh token {RefreshToken} blacklisted successfully.", refreshToken);
        }

        public async Task<bool> RegisterAsync(RegisterDto dto)
        {
            _logger.LogInformation("Registering new user with email: {Email}", dto.Email);
            // Check if a user with the same email already exists
            var exists = await _context.Customers.AnyAsync(x => x.Email == dto.Email);
            if (exists)
            {
                _logger.LogWarning("Registration failed - user with email {Email} already exists.", dto.Email);
                return false;
            }

            // Generate unique customer number
            var customerNumber = await CustomerNumberHelper.GenerateCustomerNumberAsync(_uniqueNumberChecker);
            if (string.IsNullOrEmpty(customerNumber))
                return false;

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
            _logger.LogInformation("User registered successfully with email: {Email}", dto.Email);
            return true;
        }

        public async Task<int?> GetCustomerIdFromAccessTokenAsync(string accessToken)
        {
            _logger.LogInformation("Getting customer ID from access token: {AccessToken}", accessToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("Access token is null or empty for customer ID lookup.");
                return null;
            }

            try
            {
                var principal = _jwtHelper.GetPrincipalFromExpiredToken(accessToken);
                var customerIdClaim = principal.FindFirstValue("CustomerId");

                if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerId))
                {
                    _logger.LogWarning("Invalid access token format for customer ID lookup: {AccessToken}", accessToken);
                    return null;
                }

                // Verify token exists in the database and is not revoked
                var session = await _context.SessionToken
                    .FirstOrDefaultAsync(s => s.AccessToken == accessToken && !s.IsRevoked);

                if (session == null)
                {
                    _logger.LogWarning("Access token not found in session for customer ID lookup: {AccessToken}", accessToken);
                    return null;
                }

                _logger.LogInformation("Customer ID {CustomerID} found for access token: {AccessToken}", customerId, accessToken);
                return customerId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer ID from access token: {AccessToken}", accessToken);
                return null;
            }
        }

        public async Task<string?> GetUserEmailByAccessTokenAsync(string accessToken)
        {
            _logger.LogInformation("Getting user email from access token: {AccessToken}", accessToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("Access token is null or empty for user email lookup.");
                return null;
            }

            try
            {
                var principal = _jwtHelper.GetPrincipalFromExpiredToken(accessToken);
                var email = principal.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Email not found in access token for user email lookup: {AccessToken}", accessToken);
                    return null;
                }

                // Verify token exists in the database and is not revoked
                var session = await _context.SessionToken
                    .Include(s => s.Customer)
                    .FirstOrDefaultAsync(s => s.AccessToken == accessToken && !s.IsRevoked);

                if (session == null)
                {
                    _logger.LogWarning("Access token not found in session for user email lookup: {AccessToken}", accessToken);
                    return null;
                }

                _logger.LogInformation("User email {Email} found for access token: {AccessToken}", email, accessToken);
                return email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user email from access token: {AccessToken}", accessToken);
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
                                       rt.RefreshTokenExpiration > DateTimeOffset.UtcNow && 
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
                    CreatedAt = DateTimeOffset.UtcNow
                });

                customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                customer.PasswordChangedAt = DateTimeOffset.UtcNow;
        
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
                customer.EmailLastUpdatedAt = DateTimeOffset.UtcNow;
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