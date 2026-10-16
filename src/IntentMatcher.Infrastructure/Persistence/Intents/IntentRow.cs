namespace IntentMatcher.Infrastructure.Persistence.Intents;

using Pgvector;

// Persistence model of the intents table.
public sealed record IntentRow
{
    public string IntentId { get; set; } = "";
    public string IntentName { get; set; } = "";
    public Vector? NameEmbedding { get; set; }
    public Vector? CentroidEmbedding { get; set; }
    public int ExampleCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CentroidUpdatedAt { get; set; }
}

// Projection returned by the nearest-intent search.
public sealed record IntentMatchRow
{
    public long Rank { get; set; }
    public string IntentId { get; set; } = "";
    public string IntentName { get; set; } = "";
    public double Similarity { get; set; }
}
