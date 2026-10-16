namespace IntentMatcher.Infrastructure.Persistence.LabelledQueries;

using Dapper;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Queries;
using Npgsql;

internal sealed class LabelledQueryRepository(NpgsqlDataSource dataSource)
    : ILabelledQueryRepository
{
    public async Task<LabelledQuery?> FindByText(QueryText text, CancellationToken ct = default)
    {
        var sql = LabelledQueriesSchema.Sql.SelectByText;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<LabelledQueryRow>(
            new CommandDefinition(sql, new { QueryText = text.Value }, cancellationToken: ct)
        );
        return row?.MapToLabelledQuery();
    }

    public async Task<IReadOnlyList<LabelledQuery>> ListWithoutEmbedding(
        CancellationToken ct = default
    )
    {
        var sql = LabelledQueriesSchema.Sql.SelectWithoutEmbedding;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<LabelledQueryRow>(
            new CommandDefinition(sql, cancellationToken: ct)
        );
        return rows.Select(r => r.MapToLabelledQuery()).ToList();
    }

    public async Task SaveEmbeddings(
        IReadOnlyCollection<LabelledQuery> queries,
        CancellationToken ct = default
    )
    {
        var sql = LabelledQueriesSchema.Sql.UpdateEmbeddings;
        var embedded = queries.Where(q => q.HasEmbedding).ToList();
        if (embedded.Count == 0)
            return;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    QueryIds = embedded.Select(q => q.Id).ToArray(),
                    Embeddings = embedded.Select(q => q.Embedding!.ToVectorLiteral()).ToArray(),
                },
                cancellationToken: ct
            )
        );
    }
}

internal static class LabelledQueryRowExtensions
{
    public static LabelledQuery MapToLabelledQuery(this LabelledQueryRow row) =>
        new(
            row.QueryId,
            QueryText.Create(row.QueryText),
            row.Split.ToDatasetSplit(),
            IntentId.Create(row.IntentId),
            row.Embedding?.ToEmbedding()
        );
}
