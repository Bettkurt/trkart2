using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.Models;
using TRKart.Repository.Repositories;
using TRKart.Entities.DTOs;

namespace TRKart.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IInputValidationService _inputValidationService;
        private readonly ApplicationDbContext _context;

        public TransactionController(ITransactionService transactionService, IInputValidationService inputValidationService, ApplicationDbContext context)
        {
            _transactionService = transactionService;
            _inputValidationService = inputValidationService;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddTransaction([FromBody] TransactionCreateDto dto)
        {
            try
            {
                var result = await _transactionService.AddTransactionAsync(dto);
                return Ok(new { 
                    success = true, 
                    message = "Transaction completed successfully", 
                    transaction = result 
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
            catch (System.Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while processing the transaction",
                    error = "INTERNAL_ERROR"
                });
            }
        }



        [HttpGet("validate-amount")]
        public IActionResult ValidateAmount([FromQuery] string value)
        {
            try
            {
                // Simple validation: check if amount is a positive number
                if (string.IsNullOrWhiteSpace(value))
                {
                    return Ok(new { 
                        success = true, 
                        isValid = false, 
                        message = "Amount value is required" 
                    });
                }

                if (!decimal.TryParse(value, out decimal amount))
                {
                    return Ok(new { 
                        success = true, 
                        isValid = false, 
                        message = "Amount must be a valid number" 
                    });
                }

                if (amount <= 0)
                {
                    return Ok(new { 
                        success = true, 
                        isValid = false, 
                        message = "Amount must be a positive number" 
                    });
                }

                return Ok(new { 
                    success = true, 
                    isValid = true, 
                    message = "Amount is valid" 
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while validating amount",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        [HttpPost("check-feasibility")]
        public async Task<IActionResult> CheckTransactionFeasibility([FromBody] TransactionCreateDto dto)
        {
            try
            {
                var feasibilityCheck = await _transactionService.CheckTransactionFeasibilityAsync(dto);
                return Ok(new { 
                    success = true, 
                    feasibility = feasibilityCheck 
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while checking transaction feasibility",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        [HttpGet("balance/{cardId}")]
        public async Task<IActionResult> GetCardBalance(int cardId)
        {
            try
            {
                var balance = await _transactionService.GetCardBalanceAsync(cardId);
                return Ok(new { 
                    success = true, 
                    cardId = cardId,
                    balance = balance 
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while retrieving card balance",
                    error = "INTERNAL_ERROR"
                });
            }
        }

        [HttpGet("by-card/{cardNumber}")]
        public async Task<IActionResult> GetTransactionsByCardId(string cardNumber)
        {
            var card = await _context.UserCard.FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
            if (card == null) return NotFound();
            var transactions = await _transactionService.GetTransactionsByCardIdAsync(card.CardID);
            return Ok(transactions);
        }

        [HttpGet("by-customer/{customerId}")]
        public async Task<IActionResult> GetTransactionsByCustomerId(int customerId)
        {
            var transactions = await _transactionService.GetTransactionsByCustomerIdAsync(customerId);
            return Ok(transactions);
        }

        [HttpGet("check-card-exists/{cardId}")]
        public async Task<IActionResult> CheckCardExists(int cardId)
        {
            try
            {
                var card = await _context.UserCard
                    .Where(c => c.CardID == cardId)
                    .Select(c => new { 
                        c.CardID, 
                        c.CardNumber, 
                        c.Balance, 
                        c.CardStatus,
                        c.CustomerID 
                    })
                    .FirstOrDefaultAsync();

                if (card == null)
                {
                    return Ok(new { 
                        success = true, 
                        exists = false, 
                        message = $"Card with ID {cardId} does not exist in the database" 
                    });
                }

                return Ok(new { 
                    success = true, 
                    exists = true, 
                    message = $"Card with ID {cardId} exists in the database",
                    card = card
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while checking card existence",
                    error = "INTERNAL_ERROR"
                });
            }
        }
    }
} 
