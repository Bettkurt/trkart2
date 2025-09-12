using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TRKart.Business.Services;
using TRKart.DataAccess;
using TRKart.Entities.DTOs;
using TRKart.Entities.Models;
using TRKart.Repository.Interfaces;
using TRKart.Repository.Repositories;
using Xunit;

namespace TRKart.UnitTests.BusinessTests
{
	public class TransferServiceTests
	{
		private static ApplicationDbContext CreateInMemoryContext(string dbName)
		{
			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase(dbName)
				.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
				.Options;
			return new ApplicationDbContext(options);
		}

		private static TransferService CreateService(ApplicationDbContext context)
		{
			var repo = new TestTransactionRepository(context);
			var validator = new InputValidationService();
			return new TransferService(repo, context, validator);
		}

		// Test-specific repository that handles DatabaseGenerated properties for in-memory database
		private class TestTransactionRepository : ITransactionRepository
		{
			private readonly ApplicationDbContext _context;

			public TestTransactionRepository(ApplicationDbContext context)
			{
				_context = context;
			}

			public async Task<Transaction> AddTransactionAsync(Transaction transaction)
			{
				// Set default values for DatabaseGenerated properties in test environment
				if (string.IsNullOrEmpty(transaction.TransactionStatus))
				{
					transaction.TransactionStatus = "Pending";
				}
				if (transaction.TransactionDate == default)
				{
					transaction.TransactionDate = DateTimeOffset.UtcNow;
				}

				_context.Transaction.Add(transaction);
				await _context.SaveChangesAsync();
				return transaction;
			}

			public async Task<IEnumerable<Transaction>> GetTransactionsByCardIdAsync(int cardId)
			{
				return await _context.Transaction
					.Include(t => t.UserCard)
					.Where(t => t.CardID == cardId)
					.OrderBy(t => t.UserCard.CardNumber)
					.ToListAsync();
			}

			public async Task<IEnumerable<Transaction>> GetTransactionsByCustomerIdAsync(int customerId)
			{
				return await _context.Transaction
					.Include(t => t.UserCard)
					.Where(t => t.UserCard.CustomerID == customerId)
					.OrderBy(t => t.UserCard.CardNumber)
					.ToListAsync();
			}

			public async Task<decimal> GetCardBalanceAsync(int cardId)
			{
				var card = await _context.UserCard
					.Where(c => c.CardID == cardId)
					.Select(c => c.Balance)
					.FirstOrDefaultAsync();
				
				return card;
			}

			public async Task<bool> IsCardActiveAsync(int cardId)
			{
				var card = await _context.UserCard
					.Where(c => c.CardID == cardId)
					.Select(c => c.CardStatus)
					.FirstOrDefaultAsync();
				
				return card == "Active";
			}
		}



		private static UserCard SeedCard(ApplicationDbContext context, int cardId, string number, decimal balance, string status = "Active", int customerId = 1)
		{
			var card = new UserCard
			{
				CardID = cardId,
				CardNumber = number,
				Balance = balance,
				CardStatus = status,
				CustomerID = customerId
			};
			context.UserCard.Add(card);
			context.SaveChanges();
			return card;
		}

		[Fact]
		public async Task CreateTransferAsync_Succeeds_AndCreatesLinkedTransactions()
		{
			using var context = CreateInMemoryContext(nameof(CreateTransferAsync_Succeeds_AndCreatesLinkedTransactions));
			var sender = SeedCard(context, 1, "TRK-1", 100);
			var recipient = SeedCard(context, 2, "TRK-2", 10);
			var service = CreateService(context);

			var dto = new TransferCreateDto
			{
				SenderCardID = sender.CardID,
				RecipientCardNumber = recipient.CardNumber,
				Amount = 25
			};

			var result = await service.CreateTransferAsync(dto);

			Assert.True(result.Success);
			Assert.NotNull(result.TransferOutTransaction);
			Assert.NotNull(result.TransferInTransaction);
			Assert.Equal(result.TransferOutTransaction!.TransactionID, result.TransferInTransaction!.TransferTransactionID);
			Assert.Equal(result.TransferInTransaction.TransactionID, result.TransferOutTransaction.TransferTransactionID);
		}

		[Fact]
		public async Task CreateTransferAsync_Fails_WhenInsufficientSenderBalance()
		{
			using var context = CreateInMemoryContext(nameof(CreateTransferAsync_Fails_WhenInsufficientSenderBalance));
			var sender = SeedCard(context, 10, "TRK-10", 5);
			var recipient = SeedCard(context, 11, "TRK-11", 10);
			var service = CreateService(context);

			var dto = new TransferCreateDto
			{
				SenderCardID = sender.CardID,
				RecipientCardNumber = recipient.CardNumber,
				Amount = 25
			};

			var result = await service.CreateTransferAsync(dto);
			Assert.False(result.Success);
			Assert.Contains("Insufficient balance", result.Message);
		}

		[Fact]
		public async Task ValidateRecipientCardAsync_ReturnsInvalid_ForInactiveCard()
		{
			using var context = CreateInMemoryContext(nameof(ValidateRecipientCardAsync_ReturnsInvalid_ForInactiveCard));
			SeedCard(context, 20, "TRK-20", 10, status: "Inactive");
			var service = CreateService(context);

			var result = await service.ValidateRecipientCardAsync("TRK90");
			Assert.True(result.Success);
			Assert.False(result.IsValid);
			Assert.Equal("Card is not active", result.Message);
		}
	}
}


