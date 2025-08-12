using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TRKart.Core.Interfaces;

namespace TRKart.Core.Helpers
{
    public static class CustomerNumberHelper
    {
        private const string PREFIX = "C";
        private const int RANDOM_DIGITS = 8; // 8 random digits + 1 check digit = 9 digits after 'C'
        private const int MAX_RETRY_ATTEMPTS = 10;

        public static async Task<string> GenerateCustomerNumberAsync(IUniqueNumberChecker numberChecker)
        {
            if (numberChecker == null)
            {
                throw new ArgumentNullException(nameof(numberChecker));
            }

            string customerNumber;
            bool isUnique;
            int attempts = 0;

            do
            {
                if (attempts >= MAX_RETRY_ATTEMPTS)
                {
                    throw new InvalidOperationException("Failed to generate a unique customer number after multiple attempts");
                }

                // Generate base number (8 random digits)
                string baseNumber = GenerateRandomDigits(RANDOM_DIGITS);
                
                // Calculate Luhn check digit using the helper
                int checkDigit = LuhnHelper.CalculateLuhnCheckDigit(baseNumber);
                
                // Combine prefix, base number, and check digit (C + 8 digits + 1 check digit = 10 characters total)
                customerNumber = PREFIX + baseNumber + checkDigit;
                
                // Check if customer number is unique
                isUnique = await numberChecker.IsCustomerNumberUniqueAsync(customerNumber);
                
                attempts++;
                
            } while (!isUnique);

            return customerNumber;
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
