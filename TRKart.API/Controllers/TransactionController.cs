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
        private readonly ApplicationDbContext _context;

        public TransactionController(ITransactionService transactionService, ApplicationDbContext context)
        {
            _transactionService = transactionService;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddTransaction([FromBody]TransactionCreateDto dto)
        {
            var result = await _transactionService.AddTransactionAsync(dto);
            return Ok(result);
        }

        [HttpGet("by-card/{cardNumber}")]
        public async Task<IActionResult> GetTransactionsByCardId(string cardNumber)
        {
            var card = await _context.UserCard.FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
            if (card == null) return NotFound();
            var transactions = await _context.Transaction.Where(t => t.CardID == card.CardID).ToListAsync();
            return Ok(transactions);
        }

        [HttpPut("by-card/{CardId}")]
        public async Task<IActionResult> UpdateTransaction(int CardId, [FromBody] Transaction transaction)
        {
            if (CardId != transaction.CardID)
                return BadRequest();
            var result = await _transactionService.UpdateTransactionAsync(transaction);
            if (!result)
                return NotFound();
            return NoContent();
        }

        [HttpDelete("by-card/{CardId}")]
        public async Task<IActionResult> DeleteTransaction(int CardId)
        {
            var result = await _transactionService.DeleteTransactionAsync(CardId);
            if (!result)
                return NotFound();
            return NoContent();
        }
    }
} 
