using System;
using System.Linq;
using System.Threading.Tasks;
using TRKart.Core.Interfaces;

namespace TRKart.Core.Helpers
{
    public static class WalletNumberHelper
    {
        private const string PREFIX = "W";
        private const int DIGIT_COUNT = 8;
        private const int MAX_ATTEMPTS = 10;

        public static async Task<string> GenerateWalletNumberAsync(IUniqueNumberChecker numberChecker)
        {
            if (numberChecker == null)
                throw new ArgumentNullException(nameof(numberChecker));

            string walletNumber;
            bool isUnique;
            int attempts = 0;

            do
            {
                if (attempts >= MAX_ATTEMPTS)
                    throw new InvalidOperationException("Failed to generate a unique wallet number after multiple attempts.");

                // Generate base number (without flag digit)
                string baseNumber = GenerateRandomDigits(DIGIT_COUNT);
                
                // Calculate Luhn check digit
                char flagDigit = CalculateLuhnCheckDigit(baseNumber);
                
                // Combine components
                walletNumber = $"{PREFIX}{baseNumber}{flagDigit}";
                
                // Verify uniqueness
                isUnique = await numberChecker.IsWalletNumberUniqueAsync(walletNumber);
                
                attempts++;
                
            } while (!isUnique);

            return walletNumber;
        }

        private static string GenerateRandomDigits(int length)
        {
            var random = new Random();
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static char CalculateLuhnCheckDigit(string number)
        {
            int sum = 0;
            bool alternate = true;
            
            // Process each digit from right to left
            for (int i = number.Length - 1; i >= 0; i--)
            {
                int digit = number[i] - '0';
                
                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit = (digit % 10) + 1;
                    }
                }
                
                sum += digit;
                alternate = !alternate;
            }
            
            // Calculate check digit that makes the sum a multiple of 10
            int checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit.ToString()[0];
        }
    }
}
