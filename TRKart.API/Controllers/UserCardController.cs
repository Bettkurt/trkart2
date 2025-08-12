using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using TRKart.Business.Interfaces;
using TRKart.Entities.DTOs;
using TRKart.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace TRKart.API.Controllers
{
    [ApiController]
    [Route("api/UserCard")]
    //[Authorize]
    [Produces("application/json")]
    public class UserCardController : ControllerBase
    {
        private readonly IUserCardService _userCardService;
        private readonly ILogger<UserCardController> _logger;

        public UserCardController(
            IUserCardService userCardService,
            ILogger<UserCardController> logger)
        {
            _userCardService = userCardService ?? throw new System.ArgumentNullException(nameof(userCardService));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all cards for the specified customer
        /// </summary>
        /// <param name="CustomerID">The ID of the customer</param>
        /// <response code="200">Returns the list of cards</response>
        [HttpGet("customer/{CustomerID}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserCardResponseDto>>> GetCardsByCustomerId(int CustomerID)
        {
            var cards = await _userCardService.GetUserCardsByCustomerIdAsync(CustomerID);
            return Ok(cards);
        }

        /// <summary>
        /// Get a specific card by card number
        /// </summary>
        /// <param name="CardNumber">The card number to look up</param>
        /// <response code="200">Returns the requested card</response>
        /// <response code="404">Card not found</response>
        [HttpGet("number/{CardNumber}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserCardResponseDto>> GetCardByNumber(string CardNumber)
        {
            var card = await _userCardService.GetUserCardByNumberAsync(CardNumber);
            return card == null 
                ? (ActionResult<UserCardResponseDto>)NotFound(new { message = "Kart bulunamadı." }) 
                : Ok(card);
        }

        /// <summary>
        /// Update the status of a card (e.g., to 'Deactivated' or 'Lost')
        /// </summary>
        /// <param name="updateDto">Contains card ID and new status</param>
        /// <response code="200">Card status updated successfully</response>
        /// <response code="400">Invalid request or validation failed</response>
        /// <response code="404">Card not found</response>
        [HttpPut("status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCardStatus([FromBody] CardStatusUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _userCardService.UpdateCardStatusAsync(updateDto);
                
                if (!result)
                {
                    return NotFound(new { message = "Card not found" });
                }

                return Ok(new { message = "Card status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating card status for card ID: {CardId}", updateDto?.CardId);
                return StatusCode(500, new { message = "An error occurred while updating the card status" });
            }
        }


        /// <summary>
        /// Create a new card for a customer
        /// </summary>
        /// <param name="createDto">Card creation details</param>
        /// <response code="201">Returns the newly created card</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserCardResponseDto>> CreateCard([FromBody] CreateUserCardDto createDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for card creation");
                return BadRequest(ModelState);
            }

            try
            {
                var createdCard = await _userCardService.CreateUserCardAsync(createDto);
                return CreatedAtAction(
                    nameof(GetCardByNumber), 
                    new { CardNumber = createdCard.CardNumber }, 
                    createdCard);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating card for customer {CustomerId}", createDto.CustomerID);
                return StatusCode(500, new { message = "Kart oluşturulurken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Get card status history
        /// </summary>
        /// <param name="cardNumber">The card number to get history for</param>
        /// <returns>List of status changes for the card</returns>
        /// <response code="200">Returns the status history</response>
        /// <response code="404">If the card is not found</response>
        [HttpGet("{cardNumber}/update-history")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<CardStatusHistoryDto>>> GetCardStatusHistory(string cardNumber)
        {
            try
            {
                var history = await _userCardService.GetCardStatusHistoryAsync(cardNumber);
                if (history == null)
                {
                    return NotFound(new { message = "Kart bulunamadı." });
                }
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status history for card {CardNumber}", cardNumber);
                return StatusCode(500, new { message = "Kart geçmişi alınırken bir hata oluştu." });
            }
        }

    }
}