using TRKart.Core.Helpers;
using Xunit;

namespace TRKart.UnitTests.CoreTests
{
	public class LuhnHelperTests
	{
		[Fact]
		public void ValidateLuhn_RoundTrip_IsConsistent()
		{
			var baseNumber = "7992739871"; // arbitrary base
			var check = LuhnHelper.CalculateLuhnCheckDigit(baseNumber);
			var full = baseNumber + check;
			Assert.True(LuhnHelper.ValidateLuhn(full));
		}

		[Fact]
		public void CalculateLuhnCheckDigit_ProducesValidNumber()
		{
			var baseNumber = "12345678"; // 8 digits
			var check = LuhnHelper.CalculateLuhnCheckDigit(baseNumber);
			var full = baseNumber + check;
			Assert.True(LuhnHelper.ValidateLuhn(full));
		}
	}
}


