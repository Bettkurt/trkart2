using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.DTOs;

namespace TRKart.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SecureTransactionController : SecureController
    {
        private readonly ITransactionService _transactionService;
        private readonly IInputValidationService _inputValidationService;
        private readonly ApplicationDbContext _context;

        public SecureTransactionController(
            ITransactionService transactionService,
            IInputValidationService inputValidationService,
            ApplicationDbContext context)
        {
            _transactionService = transactionService;
            _inputValidationService = inputValidationService;
            _context = context;
        }

        /// <summary>
        /// Test endpoint to check authentication
        /// </summary>
        [HttpGet("test-auth")]
        public IActionResult TestAuth()
        {
            var customerId = GetCurrentCustomerId();
            var sessionToken = Request.Cookies["SessionToken"];
            
            Console.WriteLine($"TestAuth called");
            Console.WriteLine($"SessionToken in controller: {!string.IsNullOrEmpty(sessionToken)}");
            Console.WriteLine($"CustomerID from GetCurrentCustomerId: {customerId}");
            Console.WriteLine($"All cookies: {string.Join(", ", Request.Cookies.Select(c => $"{c.Key}={c.Value}"))}");
            
            return Ok(new { 
                isAuthenticated = customerId.HasValue,
                customerId = customerId,
                sessionTokenPresent = !string.IsNullOrEmpty(sessionToken),
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
                    .Where(c => c.CustomerID == customerId.Value)
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
    }
} 