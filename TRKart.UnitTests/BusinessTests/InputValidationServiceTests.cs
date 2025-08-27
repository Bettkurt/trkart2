using System;
using System.Linq;
using TRKart.Business.Services;
using TRKart.Entities.DTOs;
using Xunit;

namespace TRKart.UnitTests.BusinessTests
{
	public class InputValidationServiceTests
	{
		private static InputValidationService CreateService()
		{
			return new InputValidationService();
		}

		[Fact]
		public void ValidateAmount_ReturnsInvalid_WhenNullOrEmpty()
		{
			var service = CreateService();
			var nullResult = service.ValidateAmount(null!);
			var emptyResult = service.ValidateAmount("");

			Assert.False(nullResult.IsValid);
			Assert.Contains(nullResult.Errors, e => e.Field == "Amount" && e.Error.Contains("cannot be empty"));

			Assert.False(emptyResult.IsValid);
			Assert.Contains(emptyResult.Errors, e => e.Field == "Amount" && e.Error.Contains("cannot be empty"));
		}

		[Theory]
		[InlineData("12a.34")]
		[InlineData("abc")]
		public void ValidateAmount_ReturnsInvalid_WhenContainsInvalidCharacters(string input)
		{
			var service = CreateService();
			var result = service.ValidateAmount(input);

			Assert.False(result.IsValid);
			Assert.Contains(result.Errors, e => e.Field == "Amount" && e.Error.Contains("invalid characters"));
		}

		[Fact]
		public void ValidateAmount_ReturnsInvalid_ForFormatIssues()
		{
			var service = CreateService();

			Assert.False(service.ValidateAmount("10.2.3").IsValid); // multiple decimals
			Assert.False(service.ValidateAmount("--10").IsValid); // multiple minus
			Assert.False(service.ValidateAmount("10-").IsValid); // minus not at start
			Assert.False(service.ValidateAmount("10.123").IsValid); // > 2 decimal places
		}

		[Fact]
		public void ValidateAmount_ReturnsInvalid_ForOutOfRange()
		{
			var service = CreateService();

			Assert.False(service.ValidateAmount("0").IsValid);
			Assert.False(service.ValidateAmount("-1").IsValid);
			Assert.False(service.ValidateAmount("1000000").IsValid);
		}

		[Theory]
		[InlineData("1")]
		[InlineData("10.5")]
		[InlineData("999999.99")]
		public void ValidateAmount_ReturnsValid_ForCorrectValues(string input)
		{
			var service = CreateService();
			var result = service.ValidateAmount(input);
			Assert.True(result.IsValid);
			Assert.Empty(result.Errors);
		}

		[Fact]
		public void ValidateTransactionType_ReturnsInvalid_WhenNullOrEmpty()
		{
			var service = CreateService();
			var nullResult = service.ValidateTransactionType(null!);
			var emptyResult = service.ValidateTransactionType("");

			Assert.False(nullResult.IsValid);
			Assert.Contains(nullResult.Errors, e => e.Field == "TransactionType" && e.Error.Contains("cannot be empty"));

			Assert.False(emptyResult.IsValid);
			Assert.Contains(emptyResult.Errors, e => e.Field == "TransactionType" && e.Error.Contains("cannot be empty"));
		}

		[Fact]
		public void ValidateTransactionType_ReturnsInvalid_WhenContainsInvalidCharacters()
		{
			var service = CreateService();
			var result = service.ValidateTransactionType("Pay!");
			Assert.False(result.IsValid);
			Assert.Contains(result.Errors, e => e.Field == "TransactionType" && e.Error.Contains("invalid characters"));
		}

		[Theory]
		[InlineData("TransferIn")]
		[InlineData("TransferOut")]
		[InlineData("Deposit")]
		public void ValidateTransactionType_ReturnsInvalid_WhenNotInAllowedList(string input)
		{
			var service = CreateService();
			var result = service.ValidateTransactionType(input);
			Assert.False(result.IsValid);
			Assert.Contains(result.Errors, e => e.Field == "TransactionType" && e.Error.Contains("must be one of"));
		}

		[Theory]
		[InlineData("Pay")]
		[InlineData("Load")]
		[InlineData("Transfer")]
		[InlineData("Refund")]
		[InlineData("pay")] // case-insensitive
		public void ValidateTransactionType_ReturnsValid_ForAllowedValues(string input)
		{
			var service = CreateService();
			var result = service.ValidateTransactionType(input);
			Assert.True(result.IsValid);
			Assert.Empty(result.Errors);
		}

		[Fact]
		public void ValidateDescription_AllowsNull_And_ValidatesRules()
		{
			var service = CreateService();

			// Null is allowed
			var nullResult = service.ValidateDescription(null!);
			Assert.True(nullResult.IsValid);
			Assert.Empty(nullResult.Errors);

			// Too long
			var longText = new string('a', 501);
			var longResult = service.ValidateDescription(longText);
			Assert.False(longResult.IsValid);
			Assert.Contains(longResult.Errors, e => e.Field == "Description" && e.Error.Contains("cannot exceed"));

			// Invalid characters (e.g., '@')
			var invalidCharsResult = service.ValidateDescription("Hello@");
			Assert.False(invalidCharsResult.IsValid);
			Assert.Contains(invalidCharsResult.Errors, e => e.Field == "Description" && e.Error.Contains("invalid characters"));

			// Valid description
			var ok = service.ValidateDescription("Payment for order 123!");
			Assert.True(ok.IsValid);
			Assert.Empty(ok.Errors);
		}

		[Fact]
		public void ValidateCardId_Validates_AllRules()
		{
			var service = CreateService();

			var nullResult = service.ValidateCardId(null!);
			Assert.False(nullResult.IsValid);
			Assert.Contains(nullResult.Errors, e => e.Field == "CardID" && e.Error.Contains("cannot be empty"));

			var nonNumeric = service.ValidateCardId("12a3");
			Assert.False(nonNumeric.IsValid);
			Assert.Contains(nonNumeric.Errors, e => e.Field == "CardID" && e.Error.Contains("invalid characters"));
			Assert.Contains(nonNumeric.Errors, e => e.Field == "CardID" && e.Error.Contains("not a valid integer"));

			var negative = service.ValidateCardId("-1");
			Assert.False(negative.IsValid);
			Assert.Contains(negative.Errors, e => e.Field == "CardID" && e.Error.Contains("positive number"));

			var zero = service.ValidateCardId("0");
			Assert.False(zero.IsValid);
			Assert.Contains(zero.Errors, e => e.Field == "CardID" && e.Error.Contains("positive number"));

			var ok = service.ValidateCardId("123");
			Assert.True(ok.IsValid);
			Assert.Empty(ok.Errors);
		}

		[Fact]
		public void ValidateTransactionInput_Aggregates_FieldValidations()
		{
			var service = CreateService();

			var dtoValid = new TransactionCreateDto
			{
				CardID = 123,
				Amount = 10.50m,
				TransactionType = "Pay",
				Description = "Coffee 2x"
			};

			var valid = service.ValidateTransactionInput(dtoValid);
			Assert.True(valid.IsValid);
			Assert.Empty(valid.Errors);

			var dtoInvalid = new TransactionCreateDto
			{
				CardID = 0, // invalid
				Amount = -5, // invalid
				TransactionType = "TransferIn", // not in allowed list
				Description = new string('x', 501) // invalid
			};

			var invalid = service.ValidateTransactionInput(dtoInvalid);
			Assert.False(invalid.IsValid);
			Assert.True(invalid.Errors.Any());
			Assert.Contains(invalid.Errors, e => e.Field == "CardID");
			Assert.Contains(invalid.Errors, e => e.Field == "Amount");
			Assert.Contains(invalid.Errors, e => e.Field == "TransactionType");
			Assert.Contains(invalid.Errors, e => e.Field == "Description");
		}
	}
}


