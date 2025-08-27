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
    public class SecureTransactionController : SecureController
    {
        private readonly ITransactionService _transactionService;
        private readonly IInputValidationService _inputValidationService;
        private readonly ITopUpService _topUpService;
        private readonly ApplicationDbContext _context;

        public SecureTransactionController(
            ITransactionService transactionService,
            IInputValidationService inputValidationService,
            ITopUpService topUpService,
            ApplicationDbContext context)
        {
            _transactionService = transactionService;
            _inputValidationService = inputValidationService;
            _topUpService = topUpService;
            _context = context;
        }

        /// <summary>
        /// Test endpoint to check authentication
        /// </summary>
        [HttpGet("test-auth")]
        public IActionResult TestAuth()
        {
            var customerId = GetCurrentCustomerId();
            var accessToken = Request.Cookies["AccessToken"];
            var refreshToken = Request.Cookies["RefreshToken"];
            
            Console.WriteLine($"TestAuth called");
            Console.WriteLine($"AccessToken in controller: {!string.IsNullOrEmpty(accessToken)}");
            Console.WriteLine($"RefreshToken in controller: {!string.IsNullOrEmpty(refreshToken)}");
            Console.WriteLine($"CustomerID from GetCurrentCustomerId: {customerId}");
            Console.WriteLine($"All cookies: {string.Join(", ", Request.Cookies.Select(c => $"{c.Key}={c.Value}"))}");
            
            return Ok(new { 
                isAuthenticated = customerId.HasValue,
                customerId = customerId,
                accessTokenPresent = !string.IsNullOrEmpty(accessToken),
                refreshTokenPresent = !string.IsNullOrEmpty(refreshToken),
                message = customerId.HasValue ? "Authentication successful" : "Authentication failed"
            });
        }

        /// <summary>
        /// Get all transactions for the authenticated user
        /// </summary>
        [HttpGet("user/transactions")]
        public async Task<IActionResult> GetUserTransactions()
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            try
            {
                var transactions = await _transactionService.GetTransactionsByCustomerIdAsync(customerId.Value);
                
                // Return simplified transaction objects to avoid circular reference
                var simplifiedTransactions = transactions.Select(t => new
                {
                    t.TransactionID,
                    t.CardID,
                    CardNumber = t.UserCard != null ? t.UserCard.CardNumber : "", // Include CardNumber for display
                    t.Amount,
                    t.TransactionType,
                    t.Description,
                    t.TransactionDate,
                    t.TransactionStatus
                }).ToList();
                
                return Ok(new { 
                    success = true, 
                    transactions = simplifiedTransactions,
                    count = simplifiedTransactions.Count()
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Failed to fetch transactions",
                    error = "INTERNAL_ERROR"
                });
            }
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
                var cards = await _context.UserCard
                    // 0 = Deactivated (As far as users concern, it is deleted for them)
                    .Where(c => c.CustomerID == customerId.Value && c.CardStatus != CardStatus.Deactivated) 
                    .Select(c => new { 
                        c.CardID, 
                        c.CardNumber, 
                        c.Balance, 
                        c.CardStatus,
                        c.CustomerID 
                    })
                    .ToListAsync();

                return Ok(new { 
                    success = true, 
                    cards = cards,
                    count = cards.Count
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Failed to fetch cards",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Create a new transaction for the authenticated user
        /// </summary>
        [HttpPost("user/transaction")]
        public async Task<IActionResult> CreateUserTransaction([FromBody] TransactionCreateDto dto)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            try
            {
                // Verify that the card belongs to the authenticated user
                var card = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardID == dto.CardID && c.CustomerID == customerId.Value);
                
                if (card == null)
                    return ForbiddenResponse("Access denied. Card does not belong to authenticated user.");

                var result = await _transactionService.AddTransactionAsync(dto);
                
                // Return simplified response to avoid circular reference
                return Ok(new { 
                    success = true, 
                    message = "Transaction completed successfully", 
                    transactionId = result.TransactionID,
                    cardId = result.CardID,
                    amount = result.Amount,
                    transactionType = result.TransactionType,
                    description = result.Description,
                    transactionDate = result.TransactionDate,
                    transactionStatus = result.TransactionStatus
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { 
                    success = false, 
                    message = ex.Message,
                    error = ex.Message.Contains("Input validation failed") ? "INPUT_VALIDATION_ERROR" : "TRANSACTION_DENIED"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while processing the transaction",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get balance for a specific card belonging to the authenticated user
        /// </summary>
        [HttpGet("user/card/{cardId}/balance")]
        public async Task<IActionResult> GetUserCardBalance(int cardId)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            try
            {
                // Verify that the card belongs to the authenticated user
                var card = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardID == cardId && c.CustomerID == customerId.Value);
                
                if (card == null)
                    return ForbiddenResponse("Access denied. Card does not belong to authenticated user.");

                var balance = await _transactionService.GetCardBalanceAsync(cardId);
                return Ok(new { 
                    success = true, 
                    cardId = cardId,
                    balance = balance 
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while retrieving card balance",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get all transactions for a specific card belonging to the authenticated user
        /// </summary>
        [HttpGet("user/card/{cardId}/transactions")]
        public async Task<IActionResult> GetUserCardTransactions(int cardId)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            try
            {
                // Verify that the card belongs to the authenticated user
                var card = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardID == cardId && c.CustomerID == customerId.Value);
                
                if (card == null)
                    return ForbiddenResponse("Access denied. Card does not belong to authenticated user.");

                var transactions = await _transactionService.GetTransactionsByCardIdAsync(cardId);
                
                // Return simplified transaction objects to avoid circular reference
                var simplifiedTransactions = transactions.Select(t => new
                {
                    t.TransactionID,
                    t.CardID,
                    CardNumber = t.UserCard != null ? t.UserCard.CardNumber : "", // Include CardNumber for display
                    t.Amount,
                    t.TransactionType,
                    t.Description,
                    t.TransactionDate,
                    t.TransactionStatus
                }).ToList();
                
                return Ok(new { 
                    success = true, 
                    transactions = simplifiedTransactions,
                    count = simplifiedTransactions.Count()
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while retrieving card transactions",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Check transaction feasibility for the authenticated user
        /// </summary>
        [HttpPost("user/check-feasibility")]
        public async Task<IActionResult> CheckUserTransactionFeasibility([FromBody] TransactionCreateDto dto)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            try
            {
                // Verify that the card belongs to the authenticated user
                var card = await _context.UserCard
                    .FirstOrDefaultAsync(c => c.CardID == dto.CardID && c.CustomerID == customerId.Value);
                
                if (card == null)
                    return ForbiddenResponse("Access denied. Card does not belong to authenticated user.");

                var feasibilityCheck = await _transactionService.CheckTransactionFeasibilityAsync(dto);
                return Ok(new { 
                    success = true, 
                    feasibility = feasibilityCheck 
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while checking transaction feasibility",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Process a top-up request for the authenticated user
        /// </summary>
        [HttpPost("topup")]
        public async Task<IActionResult> TopUp([FromBody] TopUpRequestDto request)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            var correlationId = Guid.NewGuid().ToString();

            try
            {
                var result = await _topUpService.TopUpAsync(request, customerId.Value, correlationId);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    // Determine appropriate status code based on error type
                    var statusCode = result.Error switch
                    {
                        "INPUT_VALIDATION_ERROR" => 400,
                        "BUSINESS_VALIDATION_ERROR" => 400,
                        "CARD_NOT_FOUND" => 404,
                        "CARD_NOT_ACTIVE" => 409,
                        "AUTHORIZATION_ERROR" => 403,
                        "DUPLICATE_EXTERNAL_REF" => 409,
                        "INTERNAL_ERROR" => 500,
                        _ => 400
                    };

                    return StatusCode(statusCode, result);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TopUpResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while processing the top-up request",
                    Error = "INTERNAL_ERROR",
                    CorrelationId = correlationId
                });
            }
        }

        /// <summary>
        /// Validate a top-up request for the authenticated user
        /// </summary>
        [HttpPost("topup/validate")]
        public async Task<IActionResult> ValidateTopUp([FromBody] TopUpRequestDto request)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            try
            {
                var validation = await _topUpService.ValidateTopUpRequestAsync(request, customerId.Value);
                
                return Ok(new { 
                    success = true, 
                    validation = validation 
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while validating the top-up request",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get top-up transactions for the authenticated user
        /// </summary>
        [HttpGet("topups")]
        public async Task<IActionResult> GetUserTopUps([FromQuery] int pageSize = 50, [FromQuery] int pageNumber = 1)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            try
            {
                var topUps = await _topUpService.GetCustomerTopUpsAsync(customerId.Value, pageSize, pageNumber);
                
                return Ok(new { 
                    success = true, 
                    topUps = topUps,
                    count = topUps.Count(),
                    pageSize = pageSize,
                    pageNumber = pageNumber
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while retrieving top-up transactions",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Simulate top-up approval for development/testing
        /// </summary>
        [HttpPost("topup/{transactionId}/simulate-approval")]
        public async Task<IActionResult> SimulateTopUpApproval(int transactionId)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
                return UnauthorizedResponse();

            var correlationId = Guid.NewGuid().ToString();

            try
            {
                // Verify the transaction belongs to the authenticated user
                var transaction = await _context.Transaction
                    .Include(t => t.UserCard)
                    .FirstOrDefaultAsync(t => t.TransactionID == transactionId 
                                            && t.TransactionType == "TopUp" 
                                            && t.UserCard.CustomerID == customerId.Value);

                if (transaction == null)
                    return ForbiddenResponse("Access denied. Top-up transaction does not belong to authenticated user.");

                var success = await _topUpService.SimulateTopUpApprovalAsync(transactionId, correlationId);
                
                if (success)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Top-up transaction approved successfully",
                        transactionId = transactionId,
                        correlationId = correlationId
                    });
                }
                else
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Failed to approve top-up transaction",
                        transactionId = transactionId,
                        correlationId = correlationId
                    });
                }
            }
            catch (Exception)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while simulating top-up approval",
                    error = "INTERNAL_ERROR",
                    correlationId = correlationId
                });
            }
        }
    }
} 