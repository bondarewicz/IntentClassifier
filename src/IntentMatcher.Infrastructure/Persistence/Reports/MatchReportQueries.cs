namespace IntentMatcher.Infrastructure.Persistence.Reports;

using Dapper;
using IntentMatcher.Application.Evaluation;
using IntentMatcher.Application.Matching;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Queries;
using Npgsql;

internal sealed class MatchReportQueries(NpgsqlDataSource dataSource)
    : IMatchReportQueries,
        IEvaluationQueries
{
    public async Task<IReadOnlyList<SplitSummary>> GetSummary(
        double ambiguousMargin,
        CancellationToken ct = default
    )
    {
        var sql = QueryMatchReportSchema.Sql.SelectSummaryBySplit;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<SplitSummaryRow>(
            new CommandDefinition(sql, new { Margin = ambiguousMargin }, cancellationToken: ct)
        );
        return rows.Select(r => new SplitSummary(
                r.Split.ToDatasetSplit(),
                r.Queries,
                r.AverageBestSimilarity,
                r.Correct,
                r.Ambiguous
            ))
            .ToList();
    }

    public async Task<EvaluationResult> Evaluate(
        IntentRepresentation representation,
        DatasetSplit split,
        double ambiguousMargin,
        CancellationToken ct = default
    )
    {
        var sql = QueryMatchReportSchema.Sql.EvaluateFor(representation);

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var row = await conn.QuerySingleAsync<EvaluationRow>(
            new CommandDefinition(
                sql,
                new { Split = split.ToDb(), Margin = ambiguousMargin },
                commandTimeout: 0,
                cancellationToken: ct
            )
        );
        return new EvaluationResult(
            representation,
            split,
            row.Queries,
            row.Top1Correct,
            row.Top3Correct,
            row.Ambiguous,
            row.AmbiguousTop1Correct
        );
    }
}
