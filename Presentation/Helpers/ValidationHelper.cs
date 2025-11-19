using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Presentation.Helpers
{
    /// <summary>
    /// Helper class for validation operations
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Check if string contains only digits
        /// </summary>
        public static bool IsDigitsOnly(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            return str.All(c => char.IsDigit(c));
        }

        /// <summary>
        /// Safely parse string to integer, returns 0 if invalid
        /// </summary>
        public static int SafeInt(string value)
        {
            if (int.TryParse(value, out int result))
                return result;
            return 0;
        }

        /// <summary>
        /// Validate if phone number has valid format
        /// </summary>
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove common separators
            string cleaned = phoneNumber.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");

            // Check if it starts with + and has only digits after
            if (cleaned.StartsWith("+"))
            {
                cleaned = cleaned.Substring(1);
            }

            // Should be between 7 and 15 digits
            return IsDigitsOnly(cleaned) && cleaned.Length >= 7 && cleaned.Length <= 15;
        }

        /// <summary>
        /// Validate email address format
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Input validation for numeric text boxes (KeyPress event)
        /// </summary>
        public static bool InputNumbers(char keyChar)
        {
            // Allow control keys (backspace, etc.)
            if (char.IsControl(keyChar))
                return false;

            // Only allow digits
            if (!char.IsDigit(keyChar))
                return true; // Block the key

            return false; // Allow the key
        }
    }
}
