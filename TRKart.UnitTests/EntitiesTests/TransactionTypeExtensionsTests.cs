using System.Linq;
using Xunit;
using TRKart.Entities.Enums;

namespace TRKart.UnitTests.EntitiesTests
{
    public class TransactionTypeExtensionsTests
    {
        [Fact]
        public void GetUserCreatableTransactionTypes_ReturnsCorrectTypes()
        {
            // Act
            var userCreatableTypes = TransactionTypeExtensions.GetUserCreatableTransactionTypes().ToList();

            // Assert
            Assert.Equal(4, userCreatableTypes.Count);
            Assert.Contains(TransactionType.Load, userCreatableTypes);
            Assert.Contains(TransactionType.TopUp, userCreatableTypes);
            Assert.Contains(TransactionType.TransferOut, userCreatableTypes);
            Assert.Contains(TransactionType.Pay, userCreatableTypes);
        }

        [Fact]
        public void GetUserViewableTransactionTypes_ReturnsAllTypes()
        {
            // Act
            var userViewableTypes = TransactionTypeExtensions.GetUserViewableTransactionTypes().ToList();

            // Assert
            var allEnumValues = Enum.GetValues<TransactionType>().ToList();
            Assert.Equal(allEnumValues.Count, userViewableTypes.Count);
            
            foreach (var enumValue in allEnumValues)
            {
                Assert.Contains(enumValue, userViewableTypes);
            }
        }

        [Theory]
        [InlineData(TransactionType.Load, true)]
        [InlineData(TransactionType.TopUp, true)]
        [InlineData(TransactionType.TransferOut, true)]
        [InlineData(TransactionType.Pay, true)]
        [InlineData(TransactionType.Refund, false)]
        [InlineData(TransactionType.TransferIn, false)]
        [InlineData(TransactionType.SystemTransferIn, false)]
        [InlineData(TransactionType.SystemTransferOut, false)]
        public void IsUserCreatable_ReturnsCorrectValue(TransactionType transactionType, bool expectedResult)
        {
            // Act
            var result = transactionType.IsUserCreatable();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(TransactionType.Load, false)]
        [InlineData(TransactionType.TopUp, false)]
        [InlineData(TransactionType.TransferOut, false)]
        [InlineData(TransactionType.Pay, false)]
        [InlineData(TransactionType.Refund, true)]
        [InlineData(TransactionType.TransferIn, true)]
        [InlineData(TransactionType.SystemTransferIn, true)]
        [InlineData(TransactionType.SystemTransferOut, true)]
        public void IsSystemOnly_ReturnsCorrectValue(TransactionType transactionType, bool expectedResult)
        {
            // Act
            var result = transactionType.IsSystemOnly();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(TransactionType.Load, "Load")]
        [InlineData(TransactionType.TopUp, "Top-Up")]
        [InlineData(TransactionType.Refund, "Refund")]
        [InlineData(TransactionType.TransferIn, "Transfer In")]
        [InlineData(TransactionType.TransferOut, "Transfer Out")]
        [InlineData(TransactionType.Pay, "Payment")]
        [InlineData(TransactionType.SystemTransferIn, "System Transfer In")]
        [InlineData(TransactionType.SystemTransferOut, "System Transfer Out")]
        public void GetDisplayName_ReturnsCorrectDisplayName(TransactionType transactionType, string expectedDisplayName)
        {
            // Act
            var result = transactionType.GetDisplayName();

            // Assert
            Assert.Equal(expectedDisplayName, result);
        }

        [Theory]
        [InlineData(TransactionType.Pay, true)]
        [InlineData(TransactionType.TransferOut, true)]
        [InlineData(TransactionType.SystemTransferOut, true)]
        [InlineData(TransactionType.Load, false)]
        [InlineData(TransactionType.TopUp, false)]
        [InlineData(TransactionType.Refund, false)]
        [InlineData(TransactionType.TransferIn, false)]
        [InlineData(TransactionType.SystemTransferIn, false)]
        public void IsDebitTransaction_ReturnsCorrectValue(TransactionType transactionType, bool expectedResult)
        {
            // Act
            var result = transactionType.IsDebitTransaction();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(TransactionType.Load, true)]
        [InlineData(TransactionType.TopUp, true)]
        [InlineData(TransactionType.Refund, true)]
        [InlineData(TransactionType.TransferIn, true)]
        [InlineData(TransactionType.SystemTransferIn, true)]
        [InlineData(TransactionType.Pay, false)]
        [InlineData(TransactionType.TransferOut, false)]
        [InlineData(TransactionType.SystemTransferOut, false)]
        public void IsCreditTransaction_ReturnsCorrectValue(TransactionType transactionType, bool expectedResult)
        {
            // Act
            var result = transactionType.IsCreditTransaction();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void UserCreatableAndSystemOnly_AreDisjoint()
        {
            // Arrange
            var userCreatable = TransactionTypeExtensions.GetUserCreatableTransactionTypes().ToList();
            
            // Act & Assert - No transaction type should be both user-creatable and system-only
            foreach (var type in userCreatable)
            {
                Assert.False(type.IsSystemOnly(), $"Transaction type {type} should not be both user-creatable and system-only");
            }

            // Verify that system-only types are not user-creatable
            var systemOnlyTypes = new[]
            {
                TransactionType.Refund,
                TransactionType.TransferIn,
                TransactionType.SystemTransferIn,
                TransactionType.SystemTransferOut
            };

            foreach (var type in systemOnlyTypes)
            {
                Assert.False(type.IsUserCreatable(), $"Transaction type {type} should not be user-creatable");
                Assert.True(type.IsSystemOnly(), $"Transaction type {type} should be system-only");
            }
        }

        [Fact]
        public void DebitAndCredit_AreDisjoint()
        {
            // Arrange
            var allTransactionTypes = System.Enum.GetValues<TransactionType>();

            // Act & Assert - No transaction type should be both debit and credit
            foreach (var type in allTransactionTypes)
            {
                var isDebit = type.IsDebitTransaction();
                var isCredit = type.IsCreditTransaction();
                
                Assert.False(isDebit && isCredit, $"Transaction type {type} cannot be both debit and credit");
                Assert.True(isDebit || isCredit, $"Transaction type {type} must be either debit or credit");
            }
        }
    }
}
