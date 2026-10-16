namespace IntentMatcher.Application.Matching;

using IntentMatcher.Domain.Queries;

public sealed record SplitSummary(DatasetSplit Split, long Queries, double AverageBestSimilarity, long Correct, long Ambiguous);

// Read side over the stored matches (query_match_report).
public interface IMatchReportQueries
{
    Task<IReadOnlyList<SplitSummary>> GetSummary(double ambiguousMargin, CancellationToken ct = default);
}
