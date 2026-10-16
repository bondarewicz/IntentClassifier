namespace IntentMatcher.Infrastructure.Persistence.LabelledQueries;

using Pgvector;

// Persistence model of the labelled_queries table.
public sealed record LabelledQueryRow
{
    public Guid QueryId { get; set; }
    public string Split { get; set; } = "";
    public string QueryText { get; set; } = "";
    public string IntentId { get; set; } = "";
    public Vector? Embedding { get; set; }
    public DateTime? EmbeddedAt { get; set; }
}
