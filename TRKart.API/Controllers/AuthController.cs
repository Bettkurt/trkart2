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
            var tokenOrEmail = await _authService.LoginAsync(dto);
            if (tokenOrEmail == null)
                return Unauthorized("Geçersiz e-posta veya şifre.");

            return Ok(new { message = "Giriş başarılı!", token = tokenOrEmail });
        }
    }
}
