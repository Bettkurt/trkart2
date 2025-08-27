using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TRKart.DataAccess;
using TRKart.DataAccess.Services;
using TRKart.Entities.Models;
using Xunit;

namespace TRKart.UnitTests.DataAccessTests
{
	public class UniqueNumberCheckerTests
	{
		private static ApplicationDbContext CreateInMemoryContext(string dbName)
		{
			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase(dbName)
				.Options;
			return new ApplicationDbContext(options);
		}

		[Fact]
		public async Task IsCardNumberUniqueAsync_ReturnsFalse_WhenExists()
		{
			using var context = CreateInMemoryContext(nameof(IsCardNumberUniqueAsync_ReturnsFalse_WhenExists));
			context.UserCard.Add(new UserCard { CardNumber = "TRK-123", Balance = 0, CardStatus = "Active", CustomerID = 1 });
			await context.SaveChangesAsync();

			var checker = new UniqueNumberChecker(context);
			var unique = await checker.IsCardNumberUniqueAsync("TRK-123");
			Assert.False(unique);
		}

		[Fact]
		public async Task IsCardNumberUniqueAsync_ReturnsTrue_WhenNotExists()
		{
			using var context = CreateInMemoryContext(nameof(IsCardNumberUniqueAsync_ReturnsTrue_WhenNotExists));
			var checker = new UniqueNumberChecker(context);
			var unique = await checker.IsCardNumberUniqueAsync("TRK-999");
			Assert.True(unique);
		}

		[Fact]
		public async Task IsCustomerNumberUniqueAsync_ReturnsFalse_WhenExists()
		{
			using var context = CreateInMemoryContext(nameof(IsCustomerNumberUniqueAsync_ReturnsFalse_WhenExists));
			context.Customers.Add(new Customers { CustomerNumber = "C123456789", Email = "a@b.com", FullName = "A B", PasswordHash = "x" });
			await context.SaveChangesAsync();

			var checker = new UniqueNumberChecker(context);
			var unique = await checker.IsCustomerNumberUniqueAsync("C123456789");
			Assert.False(unique);
		}

		[Fact]
		public async Task IsCustomerNumberUniqueAsync_ReturnsTrue_WhenNotExists()
		{
			using var context = CreateInMemoryContext(nameof(IsCustomerNumberUniqueAsync_ReturnsTrue_WhenNotExists));
			var checker = new UniqueNumberChecker(context);
			var unique = await checker.IsCustomerNumberUniqueAsync("C000000001");
			Assert.True(unique);
		}
	}
}


