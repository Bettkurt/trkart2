using global::TRKart.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
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
            if (!result)
                return BadRequest("Bu e-posta adresiyle zaten bir kullanıcı var.");

            return Ok("Kayıt başarılı!");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var (token, expiration) = await _authService.LoginAsync(dto);
            if (token == null)
                return Unauthorized("Geçersiz e-posta veya şifre.");
            
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
                    Secure = false, // Set to false for localhost development
                    SameSite = SameSiteMode.Lax, // More permissive for development
                    Path = "/" // Explicitly set path
                });
            
            Console.WriteLine($"Cookie set. Response headers: {string.Join(", ", Response.Headers.Select(h => $"{h.Key}={h.Value}"))}");
            
            return Ok(new { message = "Giriş başarılı!", token });
        }

        [HttpPost("login-password-only")]
        public async Task<IActionResult> LoginPasswordOnly([FromBody] PasswordOnlyLoginDto dto)
        {
            var sessionToken = Request.Cookies["SessionToken"];
            if (string.IsNullOrEmpty(sessionToken))
                return Unauthorized("No valid session found.");

            var (token, expiration) = await _authService.LoginWithPasswordOnlyAsync(sessionToken, dto.Password);
            if (token == null)
                return Unauthorized("Geçersiz şifre veya oturum süresi dolmuş.");

            // Set new cookie
            Response.Cookies.Append(
                "SessionToken",
                token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Expires = expiration,
                    Secure = false, // Set to false for localhost development
                    SameSite = SameSiteMode.Lax, // More permissive for development
                    Domain = "localhost", // Explicitly set domain
                    Path = "/" // Explicitly set path
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

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("SessionToken");
            return Ok(new { message = "Çıkış başarılı!" });
        }
    }
}
