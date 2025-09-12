using System.Net;
using System.Security.Claims;
using global::TRKart.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TRKart.DataAccess;
using TRKart.Entities.DTOs;
using Microsoft.Extensions.Logging;

namespace TRKart.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;
        private readonly IAuditService _auditService;

        public AuthController(IAuthService authService, ApplicationDbContext context, ILogger<AuthController> logger, IAuditService auditService)
        {
            _authService = authService;
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Get client information
            string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Registration attempt for email: {Email} from IP: {IPAddress}", dto.Email, ipAddress);
            
            try
            {
                var success = await _authService.RegisterAsync(dto);
                if (!success) {
                    _logger.LogWarning("Registration failed - email already exists: {Email} from IP: {IPAddress}", dto.Email, ipAddress);
                    return BadRequest("Bu e-posta adresiyle zaten bir kullanıcı var.");
                }

                _logger.LogInformation("User registered successfully: {Email} from IP: {IPAddress}", dto.Email, ipAddress);

                // Log successful registration to audit events
                try
                {
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == dto.Email);
                    if (customer != null)
                    {
                        await _auditService.LogAuditEventAsync(
                            customer.CustomerID, 
                            null, // No session ID for registration
                            new AuditEventDto
                            {
                                EventType = "REGISTRATION",
                                EventSubType = "SUCCESS",
                                EventDetails = $"User registered successfully from IP: {ipAddress}",
                                RiskLevel = "LOW"
                            },
                            ipAddress,
                            userAgent
                        );
                    }
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to log audit events for registration");
                }

                return Ok(new { message = "Kullanıcı başarıyla kaydedildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error for email: {Email} from IP: {IPAddress}", dto.Email, ipAddress);
                return StatusCode(500, "Kayıt sırasında bir hata oluştu.");
            }
        }

        [HttpPost("verify-password")]
        public async Task<IActionResult> VerifyPassword([FromBody] LoginDto dto)
        {
            _logger.LogDebug("Password verification attempt for email: {Email}", dto.Email);
            
            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
            {
                _logger.LogWarning("Password verification failed - missing credentials for email: {Email}", dto.Email);
                return BadRequest("E-posta ve şifre alanları zorunludur.");
            }

            try
            {
                bool isPasswordValid = await _authService.VerifyPasswordAsync(dto.Email, dto.Password);
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Password verification failed - invalid credentials for email: {Email}", dto.Email);
                    return Unauthorized(new { message = "Geçersiz e-posta veya şifre.", isValid = false });
                }

                _logger.LogInformation("Password verified successfully for email: {Email}", dto.Email);
                return Ok(new { message = "Password verified successfully", isValid = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password verification error for email: {Email}", dto.Email);
                return StatusCode(500, "Şifre doğrulama sırasında bir hata oluştu.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // Get client information
            string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Login attempt for email: {Email} from IP: {IPAddress}", dto.Email, ipAddress);

            try
            {
                var tokenResponse = await _authService.LoginAsync(dto, ipAddress, userAgent);
                if (tokenResponse == null) {
                    _logger.LogWarning("Login failed - invalid credentials for email: {Email} from IP: {IPAddress}", dto.Email, ipAddress);
                    
                    // Log failed login attempt to security events and update failed attempts counter
                    try
                    {
                        // Try to get customer ID for failed login attempt
                        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == dto.Email);
                        int? customerId = null;
                        int failedAttempts = 0;
                        
                        if (customer != null)
                        {
                            // Increment failed login attempts counter for existing customers
                            customer.FailedLoginAttempts++;
                            await _context.SaveChangesAsync();
                            customerId = customer.CustomerID;
                            failedAttempts = customer.FailedLoginAttempts;
                        }
                        
                        // Always log security event for failed login attempts (even for non-existent emails)
                        await _auditService.LogSecurityEventAsync(
                            customerId,
                            new SecurityEventDto
                            {
                                EventType = "LOGIN_FAILED",
                                EventSeverity = customer != null ? "MEDIUM" : "HIGH", // Higher severity for non-existent emails
                                EventDetails = customer != null 
                                    ? $"Failed login attempt for existing email: {dto.Email} from IP: {ipAddress}. Total failed attempts: {failedAttempts}"
                                    : $"Failed login attempt for non-existent email: {dto.Email} from IP: {ipAddress}",
                                Email = dto.Email, // Always include the email from the request
                                IPAddress = ipAddress,
                                UserAgent = userAgent
                            },
                            ipAddress,
                            userAgent
                        );
                    }
                    catch (Exception securityEx)
                    {
                        _logger.LogError(securityEx, "Failed to log security event for failed login");
                    }
                    
                    return Unauthorized("Geçersiz e-posta veya şifre.");
                }

                // Set access token in cookie
                Response.Cookies.Append(
                    "AccessToken",
                    tokenResponse.AccessToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Expires = tokenResponse.AccessTokenExpiration,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Path = "/"
                    });

                // Set refresh token in cookie with longer expiration
                Response.Cookies.Append(
                    "RefreshToken",
                    tokenResponse.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Expires = tokenResponse.RefreshTokenExpiration,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Path = "/"
                    });

                _logger.LogInformation("User logged in successfully: {Email} from IP: {IPAddress}", dto.Email, ipAddress);
                
                // Log successful login to audit events and reset failed attempts counter
                try
                {
                    // Get customer and session information from database
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == dto.Email);
                    var session = await _context.SessionToken
                        .FirstOrDefaultAsync(s => s.AccessToken == tokenResponse.AccessToken);
                    
                    if (customer != null && session != null)
                    {
                        // Reset failed login attempts counter and update last login time on successful login
                        if (customer.FailedLoginAttempts > 0)
                        {
                            customer.FailedLoginAttempts = 0;
                        }
                        customer.LastLoginAt = DateTimeOffset.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        await _auditService.LogAuditEventAsync(
                            customer.CustomerID, 
                            session.SessionID, 
                            new AuditEventDto
                            {
                                EventType = "LOGIN",
                                EventSubType = "SUCCESS",
                                EventDetails = $"User logged in successfully from IP: {ipAddress}",
                                RiskLevel = "LOW"
                            },
                            ipAddress,
                            userAgent
                        );
                    }
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to log audit event for successful login");
                }
                
                return Ok(new { 
                    message = "Giriş başarılı!", 
                    accessToken = tokenResponse.AccessToken,
                    refreshToken = tokenResponse.RefreshToken,
                    accessTokenExpiration = tokenResponse.AccessTokenExpiration,
                    refreshTokenExpiration = tokenResponse.RefreshTokenExpiration
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for email: {Email} from IP: {IPAddress}", dto.Email, ipAddress);
                return StatusCode(500, "Giriş sırasında bir hata oluştu.");
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            // Try to get refresh token from request body first, then from cookie
            string? refreshToken = request?.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken))
            {
                refreshToken = Request.Cookies["RefreshToken"];
            }

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Refresh token attempt failed - no token provided");
                return BadRequest("Refresh token is required");
            }

            _logger.LogDebug("Token refresh attempt with token: {TokenPrefix}...", refreshToken.Substring(0, Math.Min(10, refreshToken.Length)));

            try
            {
                // IMPORTANT: Check if refresh token is blacklisted before processing
                bool isBlacklisted = await _authService.IsRefreshTokenBlacklistedAsync(refreshToken);
                if (isBlacklisted)
                {
                    _logger.LogWarning("Refresh token attempt with blacklisted token: {TokenPrefix}...", refreshToken.Substring(0, Math.Min(10, refreshToken.Length)));
                    
                    // Clear the blacklisted refresh token cookie
                    Response.Cookies.Delete("RefreshToken", new CookieOptions {
                        Path = "/",
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    });

                    return Unauthorized("Refresh token has been revoked");
                }

                string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var tokenResponse = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

                if (tokenResponse == null)
                {
                    _logger.LogWarning("Token refresh failed - invalid or expired token from IP: {IPAddress}", ipAddress);
                    return Unauthorized("Invalid or expired refresh token");
                }

                // Set new access token in cookie
                Response.Cookies.Append(
                    "AccessToken",
                    tokenResponse.AccessToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Expires = tokenResponse.AccessTokenExpiration,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Path = "/"
                    });

                // Set new refresh token in cookie
                Response.Cookies.Append(
                    "RefreshToken",
                    tokenResponse.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Expires = tokenResponse.RefreshTokenExpiration,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Path = "/"
                    });

                _logger.LogInformation("Token refreshed successfully from IP: {IPAddress}", ipAddress);
                return Ok(new {
                    accessToken = tokenResponse.AccessToken,
                    refreshToken = tokenResponse.RefreshToken,
                    accessTokenExpiration = tokenResponse.AccessTokenExpiration,
                    refreshTokenExpiration = tokenResponse.RefreshTokenExpiration
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh error from IP: {IPAddress}", HttpContext.Connection.RemoteIpAddress?.ToString());
                return StatusCode(500, "Token yenileme sırasında bir hata oluştu.");
            }
        }

        [HttpGet("check-session")]
        public async Task<IActionResult> CheckSession()
        {
            var accessToken = Request.Cookies["AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogDebug("Session check - no access token found");
            }
            // First check if we have a refresh token
            var refreshToken = Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(refreshToken)) 
            {
                return Ok(new { hasValidSession = false, email = (string?)null, customerID = (int?)null, fullName = (string?)null });
            }

            
            try 
            {
                // If we have a refresh token, try to validate it
                var (isValid, email, customerID, fullName) = await _authService.ValidateRefreshTokenAsync(refreshToken);
                _logger.LogDebug("Session check result - Valid: {IsValid}, Email: {Email}", isValid, email);
            
                // If refresh token is valid but access token is missing/expired, issue new tokens
                if (isValid && (string.IsNullOrEmpty(Request.Cookies["AccessToken"]) || 
                    !(await _authService.ValidateAccessTokenAsync(Request.Cookies["AccessToken"])).IsValid))
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var tokenResponse = await _authService.RefreshTokenAsync(refreshToken, ipAddress);
                
                    if (tokenResponse != null)
                    {
                        // Set new access token in cookie
                        Response.Cookies.Append("AccessToken", tokenResponse.AccessToken, new CookieOptions
                        {
                            HttpOnly = true,
                            Expires = tokenResponse.AccessTokenExpiration,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Path = "/"
                        });
                    }
                    
                    return Ok(new { 
                        hasValidSession = true, 
                        email, 
                        customerID, 
                        fullName,
                        newAccessToken = tokenResponse.AccessToken
                    });
                }

                return Ok(new { 
                    hasValidSession = isValid, 
                    email, 
                    customerID, 
                    fullName 
                });
            }
             catch (Exception ex)
            {
                _logger.LogError(ex, "Session check error");
                return StatusCode(500, "Session kontrolü sırasında bir hata oluştu.");
            }
        }

        [HttpGet("user-email")]
        public async Task<IActionResult> GetUserEmailByToken([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) {  
                _logger.LogWarning("GetUserEmailByToken - no token provided");
                return BadRequest("Token is required");
            }

            try
            {
                _logger.LogDebug("GetUserEmailByToken - attempting to get email for token: {TokenPrefix}...", token.Substring(0, Math.Min(10, token.Length)));
                var email = await _authService.GetUserEmailByAccessTokenAsync(token);
                if (email == null) {
                    _logger.LogWarning("GetUserEmailByToken - no user found for token: {TokenPrefix}...", token.Substring(0, Math.Min(10, token.Length)));
                    return NotFound("No user found with the provided token");
                }

                _logger.LogDebug("GetUserEmailByToken - email found: {Email}", email);
                return Ok(new { email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserEmailByToken error for token: {TokenPrefix}...", token.Substring(0, Math.Min(10, token.Length)));
                return StatusCode(500, "Token ile kullanıcı e-postası alınırken bir hata oluştu.");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var accessToken = Request.Cookies["AccessToken"];
            _logger.LogInformation("Logout attempt for token: {TokenPrefix}...", 
                !string.IsNullOrEmpty(accessToken) ? accessToken.Substring(0, Math.Min(10, accessToken.Length)) : "none");

            try
            {
                // Get access token and update its expiration in the database
                if (!string.IsNullOrEmpty(accessToken))
                {
                    // Find the session with this access token and update its expiration
                    var session = await _context.SessionToken
                        .FirstOrDefaultAsync(s => s.AccessToken == accessToken);
                    
                    if (session != null)
                    {
                        // Log logout to audit events before expiring the session
                        try
                        {
                            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == session.CustomerID);
                            if (customer != null)
                            {
                                await _auditService.LogAuditEventAsync(
                                    customer.CustomerID,
                                    session.SessionID,
                                    new AuditEventDto
                                    {
                                        EventType = "LOGOUT",
                                        EventSubType = "SUCCESS",
                                        EventDetails = "User logged out successfully",
                                        RiskLevel = "LOW"
                                    },
                                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                                    Request.Headers["User-Agent"].ToString()
                                );
                            }
                        }
                        catch (Exception auditEx)
                        {
                            _logger.LogError(auditEx, "Failed to log audit event for logout");
                        }

                        // Blacklist the refresh token for security
                        try
                        {
                            await _authService.BlacklistRefreshTokenAsync(session.RefreshToken, "User-initiated logout");
                            _logger.LogDebug("Refresh token blacklisted for logout: {TokenPrefix}...", session.RefreshToken.Substring(0, Math.Min(10, session.RefreshToken.Length)));
                        }
                        catch (Exception blacklistEx)
                        {
                            _logger.LogError(blacklistEx, "Failed to blacklist refresh token during logout");
                        }

                        // Mark session as revoked and set access token expiration to now
                        session.IsRevoked = true;
                        session.AccessTokenExpiration = DateTimeOffset.UtcNow;
                        await _context.SaveChangesAsync();
                        _logger.LogDebug("Session expired in database for token: {TokenPrefix}...", accessToken.Substring(0, Math.Min(10, accessToken.Length)));
                    }
                }

                // Delete both cookies
                Response.Cookies.Delete("AccessToken", new CookieOptions { 
                    Path = "/", 
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });

                Response.Cookies.Delete("RefreshToken", new CookieOptions { 
                    Path = "/", 
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });

                _logger.LogInformation("User logged out successfully");
                return Ok(new { message = "Successfully logged out" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout error");
                return StatusCode(500, new { message = "An error occurred during logout", error = ex.Message });
            }
        }


        
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] TRKart.Entities.DTOs.ChangePasswordDto dto)
        {
            var accessToken = Request.Cookies["AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized();
            }
            var userEmail = await _authService.GetUserEmailByAccessTokenAsync(accessToken);
            if (userEmail == null || userEmail != dto.Email)
            {
                return Unauthorized();
            }
            var success = await _authService.ChangePasswordAsync(dto);
            if (!success)
            {
                return BadRequest();
            }
            return Ok();
        }

        [HttpPost("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDto dto)
        {
            var accessToken = Request.Cookies["AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized();
            }

            var currentEmail = await _authService.GetUserEmailByAccessTokenAsync(accessToken);
            if (string.IsNullOrEmpty(currentEmail))
            {
                return Unauthorized();
            }

            var success = await _authService.ChangeEmailAsync(currentEmail, dto);
            if (!success)
            {
                return BadRequest();
            }

            // Implicit login with new email to refresh tokens
            string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? userAgent = Request.Headers["User-Agent"].ToString();

            var loginDto = new LoginDto { Email = dto.NewEmail, Password = dto.Password };
            var tokenResponse = await _authService.LoginAsync(loginDto, ipAddress, userAgent);
            if (tokenResponse == null)
            {
                return StatusCode(500, new { message = "Email changed but re-login failed" });
            }

            // Set new tokens in cookies
            Response.Cookies.Append(
                "AccessToken",
                tokenResponse.AccessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Expires = tokenResponse.AccessTokenExpiration,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

            Response.Cookies.Append(
                "RefreshToken",
                tokenResponse.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Expires = tokenResponse.RefreshTokenExpiration,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

            return Ok(new {
                message = "Email changed and tokens refreshed",
                accessToken = tokenResponse.AccessToken,
                refreshToken = tokenResponse.RefreshToken,
                accessTokenExpiration = tokenResponse.AccessTokenExpiration,
                refreshTokenExpiration = tokenResponse.RefreshTokenExpiration
            });
        }
    }
}