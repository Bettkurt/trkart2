using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TRKart.Business.Interfaces;
using TRKart.Entities.DTOs;

namespace TRKart.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransferController : ControllerBase
    {
        private readonly ITransferService _transferService;

        public TransferController(ITransferService transferService)
        {
            _transferService = transferService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateTransfer([FromBody] TransferCreateDto dto)
        {
            try
            {
                Console.WriteLine($"TransferController: Starting transfer request for SourceId={dto.SourceId}");
                var result = await _transferService.CreateTransferAsync(dto);
                
                Console.WriteLine($"TransferController: Transfer service completed. Success: {result.Success}");
                
                if (result.Success)
                {
                    // Create a simplified response to avoid serialization issues
                    var response = new
                    {
                        success = result.Success,
                        message = result.Message,
                        transferOutTransactionId = result.TransferOutTransaction?.TransactionID,
                        transferInTransactionId = result.TransferInTransaction?.TransactionID
                    };
                    
                    Console.WriteLine($"TransferController: Returning success response");
                    return Ok(response);
                }
                else
                {
                    Console.WriteLine($"TransferController: Returning bad request: {result.Message}");
                    return BadRequest(result);
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"TransferController: Exception occurred: {ex.Message}");
                Console.WriteLine($"TransferController: Stack trace: {ex.StackTrace}");
                return StatusCode(500, new TransferResponse
                {
                    Success = false,
                    Message = "An error occurred while processing the transfer",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("validate-recipient/{cardNumber}")]
        public async Task<IActionResult> ValidateRecipientCard(string cardNumber)
        {
            try
            {
                var result = await _transferService.ValidateRecipientCardAsync(cardNumber);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new TransferValidationResponse
                {
                    Success = false,
                    IsValid = false,
                    Message = "An error occurred while validating the recipient card"
                });
            }
        }

        [HttpGet("details/{transferTransactionID}")]
        public async Task<IActionResult> GetTransferDetails(int transferTransactionID)
        {
            try
            {
                var result = await _transferService.GetTransferDetailsAsync(transferTransactionID);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return NotFound(result);
                }
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new TransferResponse
                {
                    Success = false,
                    Message = "An error occurred while retrieving transfer details",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetTransferHistory()
        {
            try
            {
                // TODO: Implement transfer history endpoint
                return Ok(new { message = "Transfer history endpoint not yet implemented" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving transfer history" });
            }
        }

        [HttpGet("history/{cardID}")]
        public async Task<IActionResult> GetTransferHistoryByCard(int cardID)
        {
            try
            {
                // TODO: Implement transfer history by card endpoint
                return Ok(new { message = "Transfer history by card endpoint not yet implemented" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving transfer history" });
            }
        }
    }
} 