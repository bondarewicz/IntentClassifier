namespace IntentMatcher.Domain.Queries;

// A customer query as it is stored, embedded and looked up: trimmed, whitespace runs collapsed to a
// single space, lowercased. Punctuation is kept because it carries meaning for intents ("?" vs ".").
// Normalizing is idempotent, so values loaded back from storage go through Create as well.
public sealed record QueryText
{
    public const int MaxLength = 1000;

    private QueryText(string value) => Value = value;

    public string Value { get; }

    public static QueryText Create(string? rawQuery)
    {
        var normalized = Normalize(rawQuery);
        if (normalized.Length == 0)
            throw new InvalidQueryException("Query is empty.");
        if (normalized.Length > MaxLength)
            throw new InvalidQueryException($"Query is longer than {MaxLength} characters.");

        return new QueryText(normalized);
    }

    public override string ToString() => Value;

    private static string Normalize(string? query) =>
        string.IsNullOrWhiteSpace(query)
            ? string.Empty
            : string.Join(' ', query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
}
