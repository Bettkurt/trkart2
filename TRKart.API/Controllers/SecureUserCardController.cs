using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.DTOs;

namespace TRKart.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SecureUserCardController : SecureController
    {
        private readonly IUserCardService _userCardService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SecureUserCardController> _logger;

        public SecureUserCardController(
            IUserCardService userCardService,
            ApplicationDbContext context,
            ILogger<SecureUserCardController> logger)
        {
            _userCardService = userCardService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all cards for the authenticated user
        /// </summary>
        [HttpGet("user/cards")]
        public async Task<IActionResult> GetUserCards()
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            try
            {
                var cards = await _userCardService.GetUserCardsByCustomerIdAsync(customerId.Value);
                return Ok(new { 
                    success = true, 
                    cards = cards,
                    count = cards.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cards for customer {CustomerId}", customerId.Value);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Failed to fetch cards",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get a specific card by card number (must belong to authenticated user)
        /// </summary>
        [HttpGet("user/card/{cardNumber}")]
        public async Task<IActionResult> GetUserCardByNumber(string cardNumber)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            try
            {
                // Verify that the card belongs to the authenticated user
                var card = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardNumber == cardNumber && c.CustomerID == customerId.Value);
                
                if (card == null)
                    return NotFound(new { 
                        success = false, 
                        message = "Card not found or access denied" 
                    });

                var cardDto = await _userCardService.GetUserCardByNumberAsync(cardNumber);
                return Ok(new { 
                    success = true, 
                    card = cardDto 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching card {CardNumber} for customer {CustomerId}", cardNumber, customerId.Value);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Failed to fetch card",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Create a new card for the authenticated user
        /// </summary>
        [HttpPost("user/card")]
        public async Task<IActionResult> CreateUserCard([FromBody] CreateUserCardDto createDto)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            try
            {
                // Ensure the card is being created for the authenticated user
                if (createDto.CustomerID != customerId.Value)
                    return ForbiddenResponse("Access denied. Cannot create card for another user.");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for card creation");
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid request data",
                        errors = ModelState
                    });
                }

                var createdCard = await _userCardService.CreateUserCardAsync(createDto);
                return CreatedAtAction(
                    nameof(GetUserCardByNumber), 
                    new { cardNumber = createdCard.CardNumber }, 
                    new { 
                        success = true, 
                        message = "Card created successfully",
                        card = createdCard 
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating card for customer {CustomerId}", customerId.Value);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Failed to create card",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Delete a card belonging to the authenticated user
        /// </summary>
        [HttpDelete("user/card")]
        public async Task<IActionResult> DeleteUserCard([FromBody] DeleteUserCardDto deleteDto)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            try
            {
                // Verify that the card belongs to the authenticated user
                var card = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardNumber == deleteDto.CardNumber && c.CustomerID == customerId.Value);
                
                if (card == null)
                    return NotFound(new { 
                        success = false, 
                        message = "Card not found or access denied" 
                    });

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for card deletion");
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid request data",
                        errors = ModelState
                    });
                }

                var result = await _userCardService.DeleteUserCardAsync(deleteDto);
                if (result)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Card deleted successfully" 
                    });
                }
                else
                {
                    return NotFound(new { 
                        success = false, 
                        message = "Card not found" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting card {CardNumber} for customer {CustomerId}", deleteDto.CardNumber, customerId.Value);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Failed to delete card",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        [HttpGet("user/profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            try
            {
                var customer = await _context.Customers
                    .Where(c => c.CustomerID == customerId.Value)
                    .Select(c => new { 
                        c.CustomerID, 
                        c.Email, 
                        c.FullName 
                    })
                    .FirstOrDefaultAsync();

                if (customer == null)
                    return NotFound(new { 
                        success = false, 
                        message = "User not found" 
                    });

                return Ok(new { 
                    success = true, 
                    user = customer 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile for customer {CustomerId}", customerId.Value);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Failed to fetch user profile",
                    error = "INTERNAL_ERROR"
                });
            }
        }
    }
} 