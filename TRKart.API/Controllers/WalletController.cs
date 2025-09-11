using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TRKart.Business.Interfaces;
using TRKart.Entities.DTOs;
using TRKart.Entities.Enums;

namespace TRKart.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly ILogger<WalletController> _logger;

        public WalletController(
            IWalletService walletService, 
            ILogger<WalletController> logger)
        {
            _walletService = walletService ?? throw new ArgumentNullException(nameof(walletService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetWalletByCustomerId(int customerId)
        {
            try
            {
                // Directly use the customer ID (primary key)
                var wallet = await _walletService.GetWalletByCustomerIdAsync(customerId);
                if (wallet == null)
                {
                    return NotFound($"No wallet found for customer ID {customerId}");
                }
                return Ok(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving wallet for customer {CustomerId}", customerId);
                return StatusCode(500, "An error occurred while retrieving the wallet.");
            }
        }

        [HttpGet("{walletId}/transactions")]
        public async Task<IActionResult> GetWalletTransactions(int walletId)
        {
            try
            {
                var transactions = await _walletService.GetWalletTransactionsAsync(walletId);
                return Ok(transactions);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Wallet not found with ID: {WalletId}", walletId);
                return NotFound($"Wallet with ID {walletId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions for wallet {WalletId}", walletId);
                return StatusCode(500, "An error occurred while retrieving wallet transactions.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletDto createWalletDto)
        {
            try
            {
                var wallet = await _walletService.CreateWalletAsync(createWalletDto);
                return CreatedAtAction(
                    nameof(GetWalletByCustomerId), 
                    new { customerId = wallet.CustomerId }, 
                    wallet);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wallet for customer {CustomerId}", createWalletDto.CustomerId);
                return StatusCode(500, "An error occurred while creating the wallet.");
            }
        }

        [HttpPut("{walletId}/status")]
        public async Task<IActionResult> UpdateWalletStatus(int walletId, [FromBody] CardStatus newStatus)
        {
            try
            {
                var wallet = await _walletService.UpdateWalletStatusAsync(walletId, newStatus);
                return Ok(wallet);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for wallet {WalletId}", walletId);
                return StatusCode(500, "An error occurred while updating the wallet status.");
            }
        }

        [HttpPost("load")]
        public async Task<IActionResult> LoadWallet([FromBody] WalletTransactionDto transactionDto)
        {
            // Enable buffering to allow reading the request body multiple times
            Request.EnableBuffering();
            
            // Read the raw request body
            string rawRequestBody;
            using (var reader = new StreamReader(
                Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true))
            {
                rawRequestBody = await reader.ReadToEndAsync();
                // Reset the request body stream position so the next middleware can read it
                Request.Body.Position = 0;
            }
            
            // Log the raw request and headers in a single log entry to reduce log noise
            _logger.LogInformation("Received wallet load request. Raw body: {RawRequestBody}", rawRequestBody);

            if (transactionDto == null)
            {
                _logger.LogWarning("LoadWallet called with null transaction DTO");
                return BadRequest(new { 
                    Success = false,
                    Message = "Transaction data is required",
                    RequestBody = rawRequestBody
                });
            }

            // Log model state if invalid
            if (!ModelState.IsValid)
            {
                // Get the actual validation error messages with field names
                var validationErrors = ModelState.Keys
                    .Where(key => ModelState[key].Errors.Count > 0)
                    .ToDictionary(
                        key => key,
                        key => ModelState[key].Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                _logger.LogWarning("Model validation failed. Errors: {Errors}. Request: {@Request}", 
                    validationErrors, 
                    new { 
                        Headers = Request.Headers,
                        Body = transactionDto,
                        RawBody = rawRequestBody
                    });
                    
                return BadRequest(new {
                    Success = false,
                    Message = "Validation failed",
                    Errors = validationErrors,
                    RequestBody = rawRequestBody
                });
            }

            _logger.LogInformation("Attempting to load wallet {WalletId} with card {CardId}, amount: {Amount}", 
                transactionDto.WalletId, transactionDto.CardId, transactionDto.Amount);

            try
            {
                // Set default transaction type if not provided
                if (string.IsNullOrEmpty(transactionDto.TransactionType))
                {
                    transactionDto.TransactionType = "Load";
                }
                
                _logger.LogDebug("Calling LoadWalletAsync with WalletId: {WalletId}, CardId: {CardId}, Amount: {Amount}", 
                    transactionDto.WalletId, transactionDto.CardId, transactionDto.Amount);
                
                var transaction = await _walletService.LoadWalletAsync(transactionDto);
                
                // Map to response DTO to avoid circular reference
                var response = new WalletLoadResponseDto
                {
                    TransactionId = transaction.TransactionID,
                    WalletId = transaction.WalletId ?? 0,
                    CardId = transaction.CardID,
                    Amount = transaction.Amount,
                    TransactionType = transaction.TransactionType != null ? transaction.TransactionType.ToString() : transactionDto.TransactionType,
                    Description = transaction.Description,
                    ReferenceId = transaction.ExternalRef,
                    TransactionDate = transaction.TransactionDate,
                    Status = !string.IsNullOrEmpty(transactionDto.Status) ? transactionDto.Status : transaction.TransactionStatus,
                    NewBalance = await _walletService.GetWalletBalanceAsync(transaction.WalletId ?? 0)
                };
                
                _logger.LogInformation("Successfully loaded {Amount} to wallet {WalletId} from card {CardId}", 
                    transactionDto.Amount, transactionDto.WalletId, transactionDto.CardId);
                    
                return Ok(new { 
                    Success = true, 
                    Data = response,
                    Message = "Wallet loaded successfully" 
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when loading wallet {WalletId}: {Message}", 
                    transactionDto.WalletId, ex.Message);
                return BadRequest(new { 
                    Success = false, 
                    Message = ex.Message 
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found when loading wallet {WalletId}: {Message}", 
                    transactionDto.WalletId, ex.Message);
                return NotFound(new { 
                    Success = false, 
                    Message = ex.Message 
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operation failed when loading wallet {WalletId}: {Message}", 
                    transactionDto.WalletId, ex.Message);
                return BadRequest(new { 
                    Success = false, 
                    Message = ex.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading wallet {WalletId}", transactionDto.WalletId);
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "An unexpected error occurred while loading the wallet.",
                    Detailed = ex.Message
                });
            }
        }

        [HttpPost("pay")]
        public async Task<IActionResult> PayFromWallet([FromBody] WalletTransactionDto transactionDto)
        {
            try
            {
                // Set default transaction type if not provided
                if (string.IsNullOrEmpty(transactionDto.TransactionType))
                {
                    transactionDto.TransactionType = "Pay";
                }
                
                var transaction = await _walletService.PayFromWalletAsync(transactionDto);
                
                // Map to response DTO to avoid circular reference
                var response = new WalletPaymentResponseDto
                {
                    TransactionId = transaction.TransactionID,
                    WalletId = transaction.WalletId ?? 0,
                    CardId = transaction.CardID,
                    Amount = transaction.Amount,
                    TransactionType = transaction.TransactionType != null ? transaction.TransactionType.ToString() : transactionDto.TransactionType,
                    Description = transaction.Description,
                    ReferenceId = transaction.ExternalRef,
                    TransactionDate = transaction.TransactionDate,
                    Status = !string.IsNullOrEmpty(transactionDto.Status) ? transactionDto.Status : transaction.TransactionStatus,
                    NewBalance = await _walletService.GetWalletBalanceAsync(transaction.WalletId ?? 0)
                };
                
                return Ok(new { 
                    Success = true,
                    Data = response,
                    Message = "Payment processed successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment from wallet {WalletId}", transactionDto.WalletId);
                return StatusCode(500, "An error occurred while processing the payment.");
            }
        }

        [HttpGet("{walletId}/balance")]
        public async Task<IActionResult> GetWalletBalance(int walletId)
        {
            try
            {
                var balance = await _walletService.GetWalletBalanceAsync(walletId);
                return Ok(new { walletId, balance });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving balance for wallet {WalletId}", walletId);
                return StatusCode(500, "An error occurred while retrieving the wallet balance.");
            }
        }
    }
}
