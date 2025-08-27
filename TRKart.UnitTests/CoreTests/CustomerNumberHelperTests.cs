using System;
using System.Linq;
using System.Threading.Tasks;
using TRKart.Core.Helpers;
using TRKart.Core.Interfaces;
using Xunit;

namespace TRKart.UnitTests.CoreTests
{
	public class CustomerNumberHelperTests
	{
		private sealed class AlwaysUniqueNumberChecker : IUniqueNumberChecker
		{
			public Task<bool> IsCardNumberUniqueAsync(string cardNumber) => Task.FromResult(true);
			public Task<bool> IsCustomerNumberUniqueAsync(string customerNumber) => Task.FromResult(true);
		}

		private sealed class FirstAttemptFailsCustomerChecker : IUniqueNumberChecker
		{
			private int _calls;
			public Task<bool> IsCardNumberUniqueAsync(string cardNumber) => Task.FromResult(true);
			public Task<bool> IsCustomerNumberUniqueAsync(string customerNumber)
			{
				_calls++;
				return Task.FromResult(_calls > 1);
			}

			public int Calls => _calls;
		}

		[Fact]
		public async Task GenerateCustomerNumberAsync_ReturnsValidFormatAndLuhn()
		{
			var number = await CustomerNumberHelper.GenerateCustomerNumberAsync(new AlwaysUniqueNumberChecker());

			Assert.False(string.IsNullOrWhiteSpace(number));
			Assert.StartsWith("C", number);

			var digits = number.Substring(1);
			Assert.True(digits.All(char.IsDigit));
			Assert.Equal(9, digits.Length); // random(8) + check(1)
			Assert.Equal(10, number.Length); // prefix(1) + 9 digits
			Assert.True(LuhnHelper.ValidateLuhn(digits));
		}

		[Fact]
		public async Task GenerateCustomerNumberAsync_RetriesUntilUnique()
		{
			var checker = new FirstAttemptFailsCustomerChecker();
			var number = await CustomerNumberHelper.GenerateCustomerNumberAsync(checker);

			Assert.False(string.IsNullOrWhiteSpace(number));
			Assert.True(checker.Calls >= 2);
		}
	}
}


