using System.Security.Claims;
using global::TRKart.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TRKart.Entities.DTOs;

namespace TRKart.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
            var (token, expiration) = await _authService.LoginAsync(dto);
            if (token == null) {
                return Unauthorized("Geçersiz e-posta veya şifre.");
            }

            Console.WriteLine($"Setting cookie for user: {dto.Email}");
            Console.WriteLine($"Token: {token.Substring(0, Math.Min(20, token.Length))}...");
            Console.WriteLine($"Expiration: {expiration}");
            
            // Set cookie
            Response.Cookies.Append(
                "SessionToken",
                token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Expires = expiration,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    // IsEssential = true
                });
            return Ok(new { message = "Giriş başarılı!", token });
        }

        [HttpGet("check-session")]
        public async Task<IActionResult> CheckSession()
        {
            var sessionToken = Request.Cookies["SessionToken"];
            if (string.IsNullOrEmpty(sessionToken))
                return Ok(new { hasValidSession = false, email = (string?)null, customerID = (int?)null, fullName = (string?)null });

            var (isValid, email, customerID, fullName) = await _authService.ValidateSessionAsync(sessionToken);
            return Ok(new { hasValidSession = isValid, email, customerID, fullName });
        }

        [HttpGet("user-email")]
        public async Task<IActionResult> GetUserEmailByToken([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) {  
                return BadRequest("Token is required");
            }

            var email = await _authService.GetUserEmailByTokenAsync(token);
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
                // Get and invalidate the session token
                var sessionToken = Request.Cookies["SessionToken"];
                if (!string.IsNullOrEmpty(sessionToken)) {
                    await _authService.InvalidateSessionAsync(sessionToken);
                }
                
                // Delete the cookie
                Response.Cookies.Delete("SessionToken", new CookieOptions { 
                    Path = "/", 
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow
                });
                return Ok(new { message = "Çıkış başarılı!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logout: {ex}");
                throw; // Re-throw to ensure we don't silently fail
            }
        }
    }
}