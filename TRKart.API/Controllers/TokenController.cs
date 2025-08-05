using Microsoft.AspNetCore.Mvc;
using TRKart.API.Attributes;
using TRKart.Business.Interfaces;
using TRKart.Entities.DTOs;

namespace TRKart.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly IAuthService _authService;

        public TokenController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest("Refresh token is required");
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var tokenResponse = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

            if (tokenResponse == null)
            {
                return Unauthorized("Invalid or expired refresh token");
            }

            return Ok(tokenResponse);
        }

        [Authorize]
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest("Refresh token is required");
            }

            var success = await _authService.RevokeTokenAsync(request.RefreshToken);

            if (!success)
            {
                return NotFound("Token not found or already revoked");
            }

            return Ok(new { message = "Token revoked successfully" });
        }

        [Authorize]
        [HttpGet("validate")]
        public async Task<IActionResult> ValidateToken()
        {
            // This endpoint just validates the token via the [Authorize] attribute
            // The JWT middleware will handle the actual token validation
            var email = HttpContext.Items["Email"] as string;
            var customerId = HttpContext.Items["CustomerId"];

            return Ok(new { isValid = true, email, customerId });
        }
    }
}
