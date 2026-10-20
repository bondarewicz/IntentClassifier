namespace IntentClassifier.Domain.Intents;

using System.Text.RegularExpressions;

// Banking77 intent label, e.g. "card_arrival". Labels are canonicalized before validation: trimmed,
// lowercased and stripped of anything but letters, digits and '_'. The dataset itself spells two
// labels inconsistently ("Refund_not_showing_up", "reverted_card_payment?").
public sealed partial record IntentId
{
    private IntentId(string value) => Value = value;

    public string Value { get; }

    public static IntentId Create(string? value)
    {
        var canonical = value is null
            ? null
            : NotAllowed().Replace(value.Trim().ToLowerInvariant(), string.Empty);
        return canonical is not null && Pattern().IsMatch(canonical)
            ? new IntentId(canonical)
            : throw new ArgumentException(
                $"'{value}' is not a valid intent id (letters, digits, '_').",
                nameof(value)
            );
    }

    // "card_arrival" -> "Card arrival": the readable name that gets embedded.
    public string ToDisplayName() => char.ToUpperInvariant(Value[0]) + Value[1..].Replace('_', ' ');

    public override string ToString() => Value;

    [GeneratedRegex("[^a-z0-9_]")]
    private static partial Regex NotAllowed();

    [GeneratedRegex("^[a-z0-9]+(_[a-z0-9]+)*$")]
    private static partial Regex Pattern();
}
