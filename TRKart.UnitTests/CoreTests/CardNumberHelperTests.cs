using System;
using System.Linq;
using System.Threading.Tasks;
using TRKart.Core.Helpers;
using TRKart.Core.Interfaces;
using Xunit;

namespace TRKart.UnitTests.CoreTests
{
	public class CardNumberHelperTests
	{
		private sealed class AlwaysUniqueNumberChecker : IUniqueNumberChecker
		{
			public Task<bool> IsCardNumberUniqueAsync(string cardNumber) => Task.FromResult(true);
			public Task<bool> IsCustomerNumberUniqueAsync(string customerNumber) => Task.FromResult(true);
		}

		private sealed class FirstAttemptFailsCardChecker : IUniqueNumberChecker
		{
			private int _calls;
			public Task<bool> IsCardNumberUniqueAsync(string cardNumber)
			{
				_calls++;
				return Task.FromResult(_calls > 1);
			}
			public Task<bool> IsCustomerNumberUniqueAsync(string customerNumber) => Task.FromResult(true);

			public int Calls => _calls;
		}

		[Fact]
		public async Task GenerateCardNumberAsync_ReturnsValidFormatAndLuhn()
		{
			var number = await CardNumberHelper.GenerateCardNumberAsync(new AlwaysUniqueNumberChecker());

			Assert.False(string.IsNullOrWhiteSpace(number));
			Assert.StartsWith("TRK", number);

			var digits = number.Substring(3);
			Assert.True(digits.All(char.IsDigit));
			Assert.StartsWith("90", digits);
			Assert.Equal(13, digits.Length); // BIN(2) + random(10) + check(1)
			Assert.Equal(16, number.Length); // prefix(3) + 13 digits
			Assert.True(LuhnHelper.ValidateLuhn(digits));
		}

		[Fact]
		public async Task GenerateCardNumberAsync_RetriesUntilUnique()
		{
			var checker = new FirstAttemptFailsCardChecker();
			var number = await CardNumberHelper.GenerateCardNumberAsync(checker);

			Assert.False(string.IsNullOrWhiteSpace(number));
			Assert.True(checker.Calls >= 2);
		}
	}
}


