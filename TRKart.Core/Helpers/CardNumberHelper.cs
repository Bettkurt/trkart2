using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TRKart.Core.Interfaces;

namespace TRKart.Core.Helpers
{
    public static class CardNumberHelper
    {
        private const string PREFIX = "TRK";
        private const string BIN = "90"; // Bank Identification Number
        private const int RANDOM_DIGITS = 10; // Number of random digits (excluding check digit)
        private const int MAX_RETRY_ATTEMPTS = 10;

        public static async Task<string> GenerateCardNumberAsync(IUniqueNumberChecker numberChecker)
        {
            if (numberChecker == null)
            {
                throw new ArgumentNullException(nameof(numberChecker));
            }

            string cardNumber;
            bool isUnique;
            int attempts = 0;

            do
            {
                if (attempts >= MAX_RETRY_ATTEMPTS)
                {
                    throw new InvalidOperationException("Failed to generate a unique card number after multiple attempts");
                }

                // Generate base number (BIN + random digits)
                string baseNumber = BIN + GenerateRandomDigits(RANDOM_DIGITS);

                // Calculate Luhn check digit using the helper
                int checkDigit = LuhnHelper.CalculateLuhnCheckDigit(baseNumber);

                // Combine prefix, base number, and check digit
                cardNumber = PREFIX + baseNumber + checkDigit;

                // Check if card number is unique
                isUnique = await numberChecker.IsCardNumberUniqueAsync(cardNumber);

                attempts++;

            } while (!isUnique);

            return cardNumber;
        }

        private static string GenerateRandomDigits(int length)
        {
            var random = new Random();
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
