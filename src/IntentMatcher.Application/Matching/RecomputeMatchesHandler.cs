namespace IntentMatcher.Application.Matching;

using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Matching;

public sealed record RecomputeMatchesResult(long RowsStored, IReadOnlyList<SplitSummary> Summary);

// Recomputes and stores every query's top-K closest intents, then summarizes the run per split.
public sealed class RecomputeMatchesHandler(
    IIntentMatchProjection matches,
    IMatchReportQueries report
)
{
    public async Task<RecomputeMatchesResult> Handle(
        int top,
        IntentRepresentation representation = IntentRepresentation.Centroid,
        CancellationToken ct = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(top, 2); // the report needs the runner-up

        var rows = await matches.RebuildAll(top, representation, ct);
        var summary = await report.GetSummary(MatchConfidence.AmbiguousMargin, ct);
        return new RecomputeMatchesResult(rows, summary);
    }
}
