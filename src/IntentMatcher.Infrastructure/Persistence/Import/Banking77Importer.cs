namespace IntentMatcher.Infrastructure.Persistence.Import;

using Dapper;
using IntentMatcher.Application.Abstractions;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Queries;
using IntentMatcher.Infrastructure.Persistence.Intents;
using IntentMatcher.Infrastructure.Persistence.LabelledQueries;
using Npgsql;

// Streams the CSVs into a temp stage with COPY FROM STDIN (client side, nothing mounted into the
// container), normalizes and validates every row with the domain types, then inserts what is missing.
// Re-runnable: existing intents and queries (and their embeddings) are kept.
internal sealed class Banking77Importer(NpgsqlDataSource dataSource) : IDatasetImporter
{
    public async Task<DatasetImportResult> ImportBanking77(
        string trainCsvPath,
        string testCsvPath,
        CancellationToken ct = default
    )
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await conn.ExecuteAsync(
            new CommandDefinition(
                Banking77ImportSchema.Sql.CreateStage,
                transaction: tx,
                cancellationToken: ct
            )
        );
        await CopyIntoStage(conn, tx, trainCsvPath, DatasetSplit.Train, ct);
        await CopyIntoStage(conn, tx, testCsvPath, DatasetSplit.Test, ct);

        var staged = (
            await conn.QueryAsync<Banking77ImportSchema.StagedRow>(
                new CommandDefinition(
                    Banking77ImportSchema.Sql.SelectStaged,
                    transaction: tx,
                    cancellationToken: ct
                )
            )
        ).ToList();

        var intents = staged
            .Select(r => IntentId.Create(r.Category))
            .Distinct()
            .OrderBy(i => i.Value, StringComparer.Ordinal)
            .ToList();

        // Duplicate texts within a split collapse to one row (first wins).
        var queries = staged
            .Select(r =>
                (
                    Split: r.Split,
                    Text: QueryText.Create(r.QueryText).Value,
                    IntentId: IntentId.Create(r.Category).Value
                )
            )
            .DistinctBy(q => (q.Split, q.Text))
            .ToList();

        await conn.ExecuteAsync(
            new CommandDefinition(
                IntentsSchema.Sql.InsertMissing,
                new
                {
                    IntentIds = intents.Select(i => i.Value).ToArray(),
                    IntentNames = intents.Select(i => i.ToDisplayName()).ToArray(),
                },
                tx,
                cancellationToken: ct
            )
        );

        await conn.ExecuteAsync(
            new CommandDefinition(
                LabelledQueriesSchema.Sql.InsertMissing,
                new
                {
                    Splits = queries.Select(q => q.Split).ToArray(),
                    QueryTexts = queries.Select(q => q.Text).ToArray(),
                    IntentIds = queries.Select(q => q.IntentId).ToArray(),
                },
                tx,
                cancellationToken: ct
            )
        );

        var counts = (
            await conn.QueryAsync<(string Split, long Count)>(
                new CommandDefinition(
                    LabelledQueriesSchema.Sql.CountBySplit,
                    transaction: tx,
                    cancellationToken: ct
                )
            )
        ).ToDictionary(c => c.Split, c => c.Count);

        await conn.ExecuteAsync(
            new CommandDefinition(
                $"ANALYZE {IntentsSchema.Table}; ANALYZE {LabelledQueriesSchema.Table};",
                transaction: tx,
                cancellationToken: ct
            )
        );
        await tx.CommitAsync(ct);

        return new DatasetImportResult(
            intents.Count,
            counts.GetValueOrDefault(DatasetSplit.Train.ToDb()),
            counts.GetValueOrDefault(DatasetSplit.Test.ToDb())
        );
    }

    private static async Task CopyIntoStage(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        string csvPath,
        DatasetSplit split,
        CancellationToken ct
    )
    {
        await using (
            var writer = await conn.BeginTextImportAsync(
                Banking77ImportSchema.Sql.CopyIntoStage,
                ct
            )
        )
        using (var reader = File.OpenText(csvPath))
        {
            var buffer = new char[81920];
            int read;
            while ((read = await reader.ReadAsync(buffer, ct)) > 0)
                await writer.WriteAsync(buffer.AsMemory(0, read), ct);
        }

        await conn.ExecuteAsync(
            new CommandDefinition(
                Banking77ImportSchema.Sql.MarkSplit,
                new { Split = split.ToDb() },
                tx,
                cancellationToken: ct
            )
        );
    }
}
