using System.Net;
using System.Text.Json;
using TRKart.Business.Interfaces;

namespace TRKart.API.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService)
{
    var accessToken = context.Request.Cookies["AccessToken"];
    var refreshToken = context.Request.Cookies["RefreshToken"];

    // Skip token validation for authentication endpoints and non-API routes
    var path = context.Request.Path.Value?.ToLower();
    if (path == null ||
        path.StartsWith("/api/auth/login") ||
        path.StartsWith("/api/auth/register") ||
        path.StartsWith("/api/auth/refresh-token") ||
        !path.StartsWith("/api/"))
    {
        await _next(context);
        return;
    }

    // If access token is missing but refresh token exists, try to refresh
    if (string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
    {
        // IMPORTANT: Check if refresh token is blacklisted before using it
        bool isBlacklisted = await authService.IsRefreshTokenBlacklistedAsync(refreshToken);

        if (!isBlacklisted)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var tokenResponse = await authService.RefreshTokenAsync(refreshToken, ipAddress);

            if (tokenResponse != null)
            {
                // Set new tokens in cookies
                context.Response.Cookies.Append(
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

                context.Response.Cookies.Append(
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

                accessToken = tokenResponse.AccessToken;
            }
        }
        else
        {
            // Refresh token is blacklisted, clear the cookies
            context.Response.Cookies.Delete("AccessToken", new CookieOptions { Path = "/" });
            context.Response.Cookies.Delete("RefreshToken", new CookieOptions { Path = "/" });
        }
    }

    // Now validate the access token
    if (!string.IsNullOrEmpty(accessToken))
    {
        var (isValid, email, customerId, fullName) = await authService.ValidateAccessTokenAsync(accessToken);

        if (isValid && customerId.HasValue)
        {
            // Store user info in HttpContext items for controllers to use
            context.Items["Email"] = email;
            context.Items["CustomerId"] = customerId.Value;
            context.Items["FullName"] = fullName;
            context.Items["IsAuthenticated"] = true;
        }
    }

    await _next(context);
}
    }

    public static class JwtMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtMiddleware>();
        }
    }
}
