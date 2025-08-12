using System;
using System.Linq;

namespace TRKart.Core.Helpers
{
    public static class LuhnHelper
    {
        /// <summary>
        /// Calculates the Luhn check digit for a given number string
        /// </summary>
        /// <param name="number">The number to calculate the check digit for (without the check digit)</param>
        /// <returns>The calculated check digit (0-9)</returns>
        public static int CalculateLuhnCheckDigit(string number)
        {
            if (string.IsNullOrEmpty(number) || !number.All(char.IsDigit))
            {
                throw new ArgumentException("Input must be a non-empty numeric string", nameof(number));
            }

            int sum = 0;
            bool alternate = false;
            
            // Process digits from right to left
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
            return checkDigit;
        }

        /// <summary>
        /// Validates if a number with its check digit is valid according to the Luhn algorithm
        /// </summary>
        /// <param name="numberWithCheckDigit">The full number including the check digit</param>
        /// <returns>True if the number is valid, false otherwise</returns>
        public static bool ValidateLuhn(string numberWithCheckDigit)
        {
            if (string.IsNullOrEmpty(numberWithCheckDigit) || numberWithCheckDigit.Length < 2 || !numberWithCheckDigit.All(char.IsDigit))
            {
                return false;
            }

            string numberWithoutCheck = numberWithCheckDigit[..^1]; // All except last character
            int checkDigit = numberWithCheckDigit[^1] - '0'; // Last character as int
            
            return CalculateLuhnCheckDigit(numberWithoutCheck) == checkDigit;
        }
    }
}
