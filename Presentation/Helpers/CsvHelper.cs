using System;
using System.IO;
using System.Linq;

namespace Presentation.Helpers
{
    /// <summary>
    /// Helper class for CSV operations
    /// </summary>
    public static class CsvHelper
    {
        /// <summary>
        /// Detect CSV delimiter (comma or semicolon) by analyzing file content
        /// </summary>
        public static string DetectDelimiter(string filePath)
        {
            if (!File.Exists(filePath))
                return ",";

            try
            {
                // Read first few lines
                string[] lines = File.ReadLines(filePath).Take(5).ToArray();

                if (lines.Length == 0)
                    return ",";

                // Count comma vs semicolon occurrences
                int commaCount = 0;
                int semicolonCount = 0;

                foreach (string line in lines)
                {
                    commaCount += line.Count(c => c == ',');
                    semicolonCount += line.Count(c => c == ';');
                }

                // Return the more common delimiter
                return semicolonCount > commaCount ? ";" : ",";
            }
            catch
            {
                return ","; // Default to comma
            }
        }

        /// <summary>
        /// Find matching header index using multiple possible names
        /// </summary>
        public static int FirstExisting(string[] headers, params string[] possibleNames)
        {
            if (headers == null || possibleNames == null)
                return -1;

            for (int i = 0; i < headers.Length; i++)
            {
                string normalized = StringHelper.NormalizeHeader(headers[i]);

                foreach (string possible in possibleNames)
                {
                    string normalizedPossible = StringHelper.NormalizeHeader(possible);

                    if (normalized == normalizedPossible ||
                        normalized.Contains(normalizedPossible) ||
                        normalizedPossible.Contains(normalized))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Parse CSV line respecting quoted fields
        /// </summary>
        public static string[] ParseCsvLine(string line, char delimiter = ',')
        {
            if (string.IsNullOrEmpty(line))
                return new string[0];

            var fields = new System.Collections.Generic.List<string>();
            bool inQuotes = false;
            var currentField = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // Toggle quote state
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        currentField.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    // End of field
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            // Add last field
            fields.Add(currentField.ToString().Trim());

            return fields.ToArray();
        }

        /// <summary>
        /// Build CSV line from fields
        /// </summary>
        public static string BuildCsvLine(params string[] fields)
        {
            if (fields == null || fields.Length == 0)
                return string.Empty;

            return string.Join(",", fields.Select(f => StringHelper.EscapeCsvField(f ?? string.Empty)));
        }

        /// <summary>
        /// Clean CSV field (remove quotes, trim)
        /// </summary>
        public static string CleanCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            field = field.Trim();

            // Remove surrounding quotes
            if (field.StartsWith("\"") && field.EndsWith("\"") && field.Length >= 2)
            {
                field = field.Substring(1, field.Length - 2);
            }

            // Unescape double quotes
            field = field.Replace("\"\"", "\"");

            return field.Trim();
        }
    }
}
