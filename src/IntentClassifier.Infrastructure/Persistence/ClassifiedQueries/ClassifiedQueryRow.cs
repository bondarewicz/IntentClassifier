namespace IntentClassifier.Infrastructure.Persistence.ClassifiedQueries;

using Pgvector;

// Persistence model of the classified_queries table.
internal sealed class ClassifiedQueryRow
{
    public Guid ClassifiedQueryId { get; set; }
    public string QueryText { get; set; } = "";
    public Vector Embedding { get; set; } = null!;
    public string Representation { get; set; } = "";
    public string? BestIntentId { get; set; }
    public double? BestSimilarity { get; set; }
    public bool Ambiguous { get; set; }
    public int TimesClassified { get; set; }
    public DateTime FirstClassifiedAt { get; set; }
    public DateTime LastClassifiedAt { get; set; }
    public string? ConfirmedIntentId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}
