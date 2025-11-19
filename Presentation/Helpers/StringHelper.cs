using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Presentation.Helpers
{
    /// <summary>
    /// Helper class for string operations
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// Encode non-ASCII characters to Unicode escape sequences
        /// </summary>
        public static string EncodeNonAscii(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    // Non-ASCII character - encode it
                    string encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Decode Unicode escape sequences to characters
        /// </summary>
        public static string DecodeNonAscii(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return Regex.Replace(
                value,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m => ((char)int.Parse(m.Groups["Value"].Value, System.Globalization.NumberStyles.HexNumber)).ToString()
            );
        }

        /// <summary>
        /// Get first N words from a string
        /// </summary>
        public static string GetWords(string text, int wordCount)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string[] words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= wordCount)
                return text;

            return string.Join(" ", words.Take(wordCount));
        }

        /// <summary>
        /// Remove suffix from string
        /// </summary>
        public static string TrimSuffix(string text, string suffix)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(suffix))
                return text;

            if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return text.Substring(0, text.Length - suffix.Length);
            }

            return text;
        }

        /// <summary>
        /// Normalize header string (trim, lowercase, remove special chars)
        /// </summary>
        public static string NormalizeHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return string.Empty;

            // Trim and lowercase
            string normalized = header.Trim().ToLower();

            // Remove special characters
            normalized = Regex.Replace(normalized, @"[^a-z0-9]", "");

            return normalized;
        }

        /// <summary>
        /// Escape CSV field (add quotes if contains comma, quote, or newline)
        /// </summary>
        public static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            // If field contains comma, quote, or newline - wrap in quotes and escape internal quotes
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        /// <summary>
        /// Truncate string to max length with ellipsis
        /// </summary>
        public static string Truncate(string text, int maxLength, string ellipsis = "...")
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength - ellipsis.Length) + ellipsis;
        }

        /// <summary>
        /// Replace placeholders in message template
        /// </summary>
        public static string ReplacePlaceholders(string template, string name, DateTime? dateTime = null)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            string result = template;

            // Replace name placeholder
            if (!string.IsNullOrWhiteSpace(name))
            {
                result = result.Replace("{name}", name);
                result = result.Replace("{Name}", name);
                result = result.Replace("{NAME}", name.ToUpper());
            }

            // Replace date/time placeholder
            if (dateTime.HasValue)
            {
                result = result.Replace("{date}", dateTime.Value.ToString("dd/MM/yyyy"));
                result = result.Replace("{time}", dateTime.Value.ToString("HH:mm:ss"));
                result = result.Replace("{datetime}", dateTime.Value.ToString("dd/MM/yyyy HH:mm:ss"));
            }

            return result;
        }
    }
}
