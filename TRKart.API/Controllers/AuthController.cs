using System.Net;
using System.Security.Claims;
using global::TRKart.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TRKart.DataAccess;
using TRKart.Entities.DTOs;

namespace TRKart.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;

        public AuthController(IAuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result) {
                return BadRequest("Bu e-posta adresiyle zaten bir kullanıcı var.");
            }

            return Ok("Kayıt başarılı!");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // Get client information
            string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? userAgent = Request.Headers["User-Agent"].ToString();

            var tokenResponse = await _authService.LoginAsync(dto, ipAddress, userAgent);
            if (tokenResponse == null) {
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

            return Ok(new { 
                message = "Giriş başarılı!", 
                accessToken = tokenResponse.AccessToken,
                refreshToken = tokenResponse.RefreshToken,
                accessTokenExpiration = tokenResponse.AccessTokenExpiration,
                refreshTokenExpiration = tokenResponse.RefreshTokenExpiration
            });
        }

        [HttpPost("refresh-token")]
public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request = null)
{
    // Try to get refresh token from request body first, then from cookie
    string? refreshToken = request?.RefreshToken;
    if (string.IsNullOrEmpty(refreshToken))
    {
        refreshToken = Request.Cookies["RefreshToken"];
    }

    if (string.IsNullOrEmpty(refreshToken))
    {
        return BadRequest("Refresh token is required");
    }

    // IMPORTANT: Check if refresh token is blacklisted before processing
    bool isBlacklisted = await _authService.IsRefreshTokenBlacklistedAsync(refreshToken);
    if (isBlacklisted)
    {
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

    return Ok(new {
        accessToken = tokenResponse.AccessToken,
        refreshToken = tokenResponse.RefreshToken,
        accessTokenExpiration = tokenResponse.AccessTokenExpiration,
        refreshTokenExpiration = tokenResponse.RefreshTokenExpiration
    });
}

        [HttpGet("check-session")]
        public async Task<IActionResult> CheckSession()
        {
            var accessToken = Request.Cookies["AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
                return Ok(new { hasValidSession = false, email = (string?)null, customerID = (int?)null, fullName = (string?)null });

            var (isValid, email, customerID, fullName) = await _authService.ValidateAccessTokenAsync(accessToken);
            return Ok(new { hasValidSession = isValid, email, customerID, fullName });
        }

        [HttpGet("user-email")]
        public async Task<IActionResult> GetUserEmailByToken([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) {  
                return BadRequest("Token is required");
            }

            var email = await _authService.GetUserEmailByAccessTokenAsync(token);
            if (email == null) {
                return NotFound("No user found with the provided token");
            }

            return Ok(new { email });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Get access token and update its expiration in the database
                var accessToken = Request.Cookies["AccessToken"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    // Find the session with this access token and update its expiration
                    var session = await _context.SessionToken
                        .FirstOrDefaultAsync(s => s.AccessToken == accessToken);
                    
                    if (session != null)
                    {
                        // Set access token expiration to now in the database
                        session.AccessTokenExpiration = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
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

                return Ok(new { message = "Successfully logged out" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during logout", error = ex.Message });
            }
        }
    }
}