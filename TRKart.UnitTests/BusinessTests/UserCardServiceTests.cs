using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TRKart.Business.Services;
using TRKart.Core.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.DTOs;
using TRKart.Entities.Models;
using TRKart.Entities.Enums;
using Xunit;

namespace TRKart.UnitTests.BusinessTests
{
	public class UserCardServiceTests
	{
		private sealed class AlwaysUniqueNumberChecker : IUniqueNumberChecker
		{
			public Task<bool> IsCardNumberUniqueAsync(string cardNumber) => Task.FromResult(true);
			public Task<bool> IsCustomerNumberUniqueAsync(string customerNumber) => Task.FromResult(true);
		}

		private static ApplicationDbContext CreateInMemoryContext(string dbName)
		{
			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase(dbName)
				.Options;
			return new ApplicationDbContext(options);
		}

		private static UserCardService CreateService(ApplicationDbContext context)
		{
			var unique = new AlwaysUniqueNumberChecker();
			return new UserCardService(context, unique);
		}

		[Fact]
		public async Task CreateUserCardAsync_CreatesInactiveCard_WithGeneratedNumber()
		{
			using var context = CreateInMemoryContext(nameof(CreateUserCardAsync_CreatesInactiveCard_WithGeneratedNumber));
			context.Customers.Add(new Customers { CustomerNumber = "CUST-001", Email = "u@test.com", FullName = "U", PasswordHash = "x" });
			await context.SaveChangesAsync();

			var service = CreateService(context);
			var dto = new CreateUserCardDto { CustomerID = 1 };
			var result = await service.CreateUserCardAsync(dto);

			Assert.NotNull(result);
			Assert.True(result.CardID > 0);
			Assert.False(string.IsNullOrWhiteSpace(result.CardNumber));
			Assert.Equal(CardStatus.Inactive, result.CardStatus);
			Assert.Equal(0, result.Balance);
		}

		[Fact]
		public async Task UpdateCardStatusAsync_UpdatesStatus()
		{
			using var context = CreateInMemoryContext(nameof(UpdateCardStatusAsync_UpdatesStatus));
			context.Customers.Add(new Customers { CustomerNumber = "CUST-002", Email = "u@test.com", FullName = "U", PasswordHash = "x" });
			await context.SaveChangesAsync();
			context.UserCard.Add(new UserCard { CustomerID = 1, CardNumber = "TRK-X", Balance = 0, CardStatus = CardStatus.Inactive });
			await context.SaveChangesAsync();

			var service = CreateService(context);
			var ok = await service.UpdateCardStatusAsync(new CardStatusUpdateDto { CardId = 1, Status = CardStatus.Active });
			Assert.True(ok);
			Assert.Equal(CardStatus.Active, (await context.UserCard.FirstAsync()).CardStatus);
		}

		[Fact]
		public async Task GetUserCardsByCustomerIdAsync_ExcludesDeactivated()
		{
			using var context = CreateInMemoryContext(nameof(GetUserCardsByCustomerIdAsync_ExcludesDeactivated));
			context.Customers.Add(new Customers { CustomerNumber = "CUST-003", Email = "u@test.com", FullName = "U", PasswordHash = "x" });
			await context.SaveChangesAsync();
			context.UserCard.Add(new UserCard { CustomerID = 1, CardNumber = "A", Balance = 0, CardStatus = CardStatus.Active });
			context.UserCard.Add(new UserCard { CustomerID = 1, CardNumber = "B", Balance = 0, CardStatus = CardStatus.Deactivated });
			await context.SaveChangesAsync();

			var service = CreateService(context);
			var list = await service.GetUserCardsByCustomerIdAsync(1);
			Assert.Single(list);
		}

		[Fact]
		public async Task GetUserCardByNumberAsync_ReturnsNull_WhenDeactivated()
		{
			using var context = CreateInMemoryContext(nameof(GetUserCardByNumberAsync_ReturnsNull_WhenDeactivated));
			context.Customers.Add(new Customers { CustomerNumber = "CUST-004", Email = "u@test.com", FullName = "U", PasswordHash = "x" });
			await context.SaveChangesAsync();
			context.UserCard.Add(new UserCard { CustomerID = 1, CardNumber = "Z", Balance = 0, CardStatus = CardStatus.Deactivated });
			await context.SaveChangesAsync();

			var service = CreateService(context);
			var card = await service.GetUserCardByNumberAsync("Z");
			Assert.Null(card);
		}

		[Fact]
		public async Task GetCardStatusHistoryAsync_ReturnsHistory()
		{
			using var context = CreateInMemoryContext(nameof(GetCardStatusHistoryAsync_ReturnsHistory));
			context.Customers.Add(new Customers { CustomerNumber = "CUST-005", Email = "u@test.com", FullName = "U", PasswordHash = "x" });
			await context.SaveChangesAsync();
			context.UserCard.Add(new UserCard { CustomerID = 1, CardNumber = "H", Balance = 0, CardStatus = CardStatus.Active, CardID = 5 });
			await context.SaveChangesAsync();
			context.CardUpdates.Add(new CardUpdates { CardID = 5, NewStatus = CardStatus.Active, PreviousStatus = CardStatus.Inactive, UpdatedAt = DateTimeOffset.UtcNow });
			await context.SaveChangesAsync();

			var service = CreateService(context);
			var history = await service.GetCardStatusHistoryAsync("H");
			Assert.NotNull(history);
			Assert.Single(history);
		}
	}
}


