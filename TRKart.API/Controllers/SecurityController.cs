using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TRKart.API.Attributes;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;

namespace TRKart.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SecurityController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;

        public SecurityController(IAuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpPost("revoke-all-sessions")]
        public async Task<IActionResult> RevokeAllSessions()
        {
            // Get the current user's ID from the token
            var customerId = HttpContext.Items["CustomerId"] as int?;
            if (!customerId.HasValue)
            {
                return Unauthorized();
            }

            // Get all active sessions for this user
            var sessions = await _context.SessionToken
                .Where(s => s.CustomerID == customerId.Value && !s.IsRevoked)
                .ToListAsync();

            // Revoke all sessions
            foreach (var session in sessions)
            {
                session.IsRevoked = true;
                await _authService.BlacklistRefreshTokenAsync(
                    session.RefreshToken, 
                    "User requested to revoke all sessions");
            }

            await _context.SaveChangesAsync();

            // Delete cookies for current session
            Response.Cookies.Delete("AccessToken", new CookieOptions { Path = "/" });
            Response.Cookies.Delete("RefreshToken", new CookieOptions { Path = "/api/auth/refresh-token" });

            return Ok(new { message = $"Successfully revoked {sessions.Count} sessions" });
        }

        [HttpGet("active-sessions")]
        public async Task<IActionResult> GetActiveSessions()
        {
            // Get the current user's ID from the token
            var customerId = HttpContext.Items["CustomerId"] as int?;
            if (!customerId.HasValue)
            {
                return Unauthorized();
            }

            // Get all active sessions for this user
            var sessions = await _context.SessionToken
                .Where(s => s.CustomerID == customerId.Value && !s.IsRevoked && s.RefreshTokenExpiration > DateTime.UtcNow)
                .Select(s => new {
                    s.SessionID,
                    s.RefreshTokenCreatedAt,
                    s.RefreshTokenExpiration,
                    s.IPAddress,
                    s.DeviceInfo
                })
                .ToListAsync();

            return Ok(sessions);
        }
    }
}
