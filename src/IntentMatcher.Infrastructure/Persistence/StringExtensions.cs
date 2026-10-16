namespace IntentMatcher.Infrastructure.Persistence;

using System.Text.RegularExpressions;

internal static partial class StringExtensions
{
    // IntentId -> intent_id (same convention as Pando's ToSnakeCase).
    public static string ToSnakeCase(this string input) =>
        string.IsNullOrEmpty(input) ? input : PascalBoundary().Replace(input, "$1_$2").ToLowerInvariant();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex PascalBoundary();
}
