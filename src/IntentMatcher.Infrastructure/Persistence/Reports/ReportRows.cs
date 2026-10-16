namespace IntentMatcher.Infrastructure.Persistence.Reports;

// Projection of the per-split summary over the query_match_report view.
public sealed record SplitSummaryRow
{
    public string Split { get; set; } = "";
    public long Queries { get; set; }
    public double AverageBestSimilarity { get; set; }
    public long Correct { get; set; }
    public long Ambiguous { get; set; }
}

// Projection of an evaluation run.
public sealed record EvaluationRow
{
    public long Queries { get; set; }
    public long Top1Correct { get; set; }
    public long Top3Correct { get; set; }
    public long Ambiguous { get; set; }
    public long AmbiguousTop1Correct { get; set; }
}
