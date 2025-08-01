using Microsoft.AspNetCore.Mvc;

namespace TRKart.API.Controllers
{
    public abstract class SecureController : ControllerBase
    {
        /// <summary>
        /// Gets the current customer ID from the authenticated session
        /// </summary>
        /// <returns>Customer ID if authenticated, null otherwise</returns>
        protected int? GetCurrentCustomerId()
        {
            // Try to get from HttpContext.Items first (from middleware)
            if (HttpContext.Items.TryGetValue("CustomerID", out var customerIdObj) && customerIdObj is int customerId)
            {
                return customerId;
            }

            // Fallback to claims if available
            var customerIdClaim = User.FindFirst("CustomerID");
            if (customerIdClaim != null && int.TryParse(customerIdClaim.Value, out var parsedCustomerId))
            {
                return parsedCustomerId;
            }

            return null;
        }

        /// <summary>
        /// Checks if the current request is authenticated
        /// </summary>
        /// <returns>True if authenticated, false otherwise</returns>
        protected bool IsAuthenticated()
        {
            return GetCurrentCustomerId().HasValue;
        }

        /// <summary>
        /// Returns an unauthorized response for unauthenticated requests
        /// </summary>
        /// <returns>Unauthorized result</returns>
        protected IActionResult UnauthorizedResponse()
        {
            return Unauthorized(new { 
                error = "Authentication required", 
                message = "No valid session found. Please log in." 
            });
        }

        /// <summary>
        /// Returns a forbidden response for unauthorized access
        /// </summary>
        /// <param name="message">Optional custom message</param>
        /// <returns>Forbidden result</returns>
        protected IActionResult ForbiddenResponse(string message = "Access denied")
        {
            return StatusCode(403, new { 
                error = "Access denied", 
                message = message 
            });
        }

        /// <summary>
        /// Ensures the request is authenticated and returns the customer ID
        /// </summary>
        /// <returns>Customer ID if authenticated, null otherwise</returns>
        protected int? RequireAuthentication()
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
            {
                throw new UnauthorizedAccessException("Authentication required");
            }
            return customerId;
        }
    }
} 