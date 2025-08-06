using TRKart.Business.Interfaces;

namespace TRKart.API.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var refreshToken = context.Request.Cookies["RefreshToken"];
                var accessToken = context.Request.Cookies["AccessToken"];
                Console.WriteLine($"Request to: {context.Request.Path}");
                Console.WriteLine($"RefreshToken present: {!string.IsNullOrEmpty(refreshToken)}");
                Console.WriteLine($"AccessToken present: {!string.IsNullOrEmpty(accessToken)}");
                
                
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    Console.WriteLine($"RefreshToken: {refreshToken.Substring(0, Math.Min(10, refreshToken.Length))}...");
                    
                    // Get IAuthService from the service provider within the request scope
                    var authService = context.RequestServices.GetService<IAuthService>();
                    if (authService != null)
                    {
                        var customerId = await authService.GetCustomerIdFromAccessTokenAsync(accessToken);
                        Console.WriteLine($"CustomerID from token: {customerId}");
                        
                        if (customerId.HasValue)
                        {
                            // Add customer ID to HttpContext for use in controllers
                            context.Items["CustomerID"] = customerId.Value;
                            
                            // Also add to claims for potential future use
                            var claims = new List<System.Security.Claims.Claim>
                            {
                                new System.Security.Claims.Claim("CustomerID", customerId.Value.ToString())
                            };
                            
                            var identity = new System.Security.Claims.ClaimsIdentity(claims, "AccessToken");
                            context.User = new System.Security.Claims.ClaimsPrincipal(identity);
                            Console.WriteLine($"Authentication successful for CustomerID: {customerId.Value}");
                        }
                        else
                        {
                            Console.WriteLine("CustomerID is null - token validation failed");
                        }
                    }
                    else
                    {
                        Console.WriteLine("IAuthService is null");
                    }
                }
                else
                {
                    Console.WriteLine("No RefreshToken found in cookies");
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the request
                // This allows non-authenticated endpoints to still work
                Console.WriteLine($"Authentication middleware error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            await _next(context);
        }
    }

    // Extension method for easy middleware registration
    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
} 