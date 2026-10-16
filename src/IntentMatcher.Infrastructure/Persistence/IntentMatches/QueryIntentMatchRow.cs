namespace IntentMatcher.Infrastructure.Persistence.IntentMatches;

// Persistence model of the query_intent_matches table.
public sealed record QueryIntentMatchRow
{
    public Guid QueryId { get; set; }
    public short Rank { get; set; }
    public string IntentId { get; set; } = "";
    public float Similarity { get; set; }
}
