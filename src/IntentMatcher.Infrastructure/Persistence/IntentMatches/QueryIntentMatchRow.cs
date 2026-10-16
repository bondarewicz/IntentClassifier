namespace IntentMatcher.Infrastructure.Persistence.IntentMatches;

// Persistence model of the query_intent_matches table.
internal sealed class QueryIntentMatchRow
{
    public Guid QueryId { get; set; }
    public int Rank { get; set; }
    public string IntentId { get; set; } = "";
    public double Similarity { get; set; }
}
