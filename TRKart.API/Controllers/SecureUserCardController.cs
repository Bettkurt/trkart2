using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.DTOs;
using TRKart.Entities.Enums;

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

               /* if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for card creation");
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid request data",
                        errors = ModelState
                    });
                } */

                // Validate CardType is within valid range
                if (!Enum.IsDefined(typeof(CardType), createDto.CardType))
                {
                    return BadRequest(new {
                        success = false,
                        message = "Invalid card type",
                        error = "INVALID_CARD_TYPE"
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
        /// Update card status (e.g., to 'Deactivated' or 'Lost')
        /// </summary>
        [HttpPut("user/card/status")]
        public async Task<IActionResult> UpdateCardStatus([FromBody] CardStatusUpdateDto updateDto)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
            {
                _logger.LogWarning("No customer ID found in the current session");
                return UnauthorizedResponse();
            }

            try
            {
                // Verify the card belongs to the current user
                var card = await _context.UserCard
                    .FirstOrDefaultAsync(uc => uc.CardID == updateDto.CardID && uc.CustomerID == customerId);
                    
                Console.WriteLine($"[UpdateCardStatus] Card found 1: {card}");

                if (card == null)
                {
                    Console.WriteLine($"[UpdateCardStatus] Card not found or you don't have permission to update this card");
                    return NotFound(new { 
                        success = false, 
                        message = "Card not found or you don't have permission to update this card" 
                    });
                }

                Console.WriteLine($"[UpdateCardStatus] Card found 2: {card}");

                // Update the card status
                var success = await _userCardService.UpdateCardStatusAsync(updateDto);
                
                Console.WriteLine($"[UpdateCardStatus] Card updated: {success}");
                
                if (!success)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Failed to update card status"
                    });
                }

                // Return a simplified response without circular references
                return Ok(new { 
                    success = true, 
                    message = "Card status updated successfully",
                    card = new {
                        CardID = card.CardID,
                        CardNumber = card.CardNumber,
                        CardStatus = updateDto.Status,
                        //LastUpdate = DateTime.UtcNow  // Set by DB
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating card status for card {CardID}", updateDto.CardID);
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while updating card status" 
                });
            }
        }

        /// <summary>
        /// Update card name
        /// </summary>
        [HttpPut("user/card/name")]
        public async Task<IActionResult> UpdateCardName([FromBody] UpdateCardNameDto updateDto)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
            {
                _logger.LogWarning("No customer ID found in the current session");
                return UnauthorizedResponse();
            }

            try
            {
                // Verify the card belongs to the current user
                var card = await _context.UserCard
                    .FirstOrDefaultAsync(uc => uc.CardID == updateDto.CardID && uc.CustomerID == customerId);

                if (card == null)
                {
                    _logger.LogWarning("Card {CardID} not found or access denied for customer {CustomerId}", updateDto.CardID, customerId.Value);
                    return NotFound(new { 
                        success = false, 
                        message = "Card not found or access denied" 
                    });
                }

                // Update the card name
                var success = await _userCardService.UpdateCardNameAsync(updateDto);
                
                if (!success)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Failed to update card name"
                    });
                }

                // Return updated card information
                return Ok(new { 
                    success = true, 
                    message = "Card name updated successfully",
                    card = new {
                        CardID = card.CardID,
                        CardNumber = card.CardNumber,
                        CardName = updateDto.CardName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating card name for card {CardID}", updateDto.CardID);
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while updating card name" 
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