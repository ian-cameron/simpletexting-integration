
using System.Text.RegularExpressions;

namespace SimpletextingAPI 
{
    public static class Utils {
        public static string StripNonNumeric(string? input)
        {
            return Regex.Replace(input ?? string.Empty, "[^0-9]", "");
        }

        public static bool IsBlank(string? input)
        {
            return string.IsNullOrWhiteSpace(input) || input.Length == 0;
        }
    }
}
