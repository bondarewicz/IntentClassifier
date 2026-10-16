namespace IntentMatcher.Infrastructure.Persistence.ClassifiedQueries;

using Dapper;
using IntentMatcher.Application.Queries;
using IntentMatcher.Domain.Embeddings;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Queries;
using IntentMatcher.Infrastructure.Persistence.Intents;
using IntentMatcher.Infrastructure.Persistence.LabelledQueries;
using Npgsql;

internal sealed class ClassifiedQueryLog(NpgsqlDataSource dataSource) : IClassifiedQueryLog
{
    public async Task<ClassifiedQuery?> FindByText(QueryText text, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<ClassifiedQueryRow>(
            new CommandDefinition(
                ClassifiedQueriesSchema.Sql.SelectByText,
                new { QueryText = text.Value },
                cancellationToken: ct
            )
        );
        return row?.MapToClassifiedQuery();
    }

    public async Task Record(
        QueryText text,
        Embedding embedding,
        IntentRepresentation representation,
        IntentId? bestIntentId,
        double? bestSimilarity,
        bool ambiguous,
        CancellationToken ct = default
    )
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            new CommandDefinition(
                ClassifiedQueriesSchema.Sql.Upsert,
                new
                {
                    QueryText = text.Value,
                    Embedding = embedding.ToVector(),
                    Representation = representation.ToString(),
                    BestIntentId = bestIntentId?.Value,
                    BestSimilarity = bestSimilarity,
                    Ambiguous = ambiguous,
                },
                cancellationToken: ct
            )
        );
    }

    // All of a label's guard checks and both its writes run under one transaction, locking the
    // log row for its duration (SelectByTextForUpdate) - a concurrent label call for the same
    // text serializes behind it instead of racing it. Promotion also writes labelled_queries;
    // the one deliberate cross-table write in Persistence (see
    // LabelledQueriesSchema.Sql.UpsertTrainExample for why that SQL still lives on that table).
    public async Task Confirm(QueryText text, IntentId intentId, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var logged = await conn.QuerySingleOrDefaultAsync<ClassifiedQueryRow>(
            new CommandDefinition(
                ClassifiedQueriesSchema.Sql.SelectByTextForUpdate,
                new { QueryText = text.Value },
                tx,
                cancellationToken: ct
            )
        );
        if (logged is null)
            throw new InvalidQueryException(
                $"'{text}' has not been classified yet - run classify first."
            );

        var intentExists = await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                IntentsSchema.Sql.Exists,
                new { IntentId = intentId.Value },
                tx,
                cancellationToken: ct
            )
        );
        if (!intentExists)
            throw new InvalidQueryException($"Intent '{intentId}' does not exist.");

        var inTestSplit = await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                LabelledQueriesSchema.Sql.ExistsBySplitAndText,
                new { Split = "test", QueryText = text.Value },
                tx,
                cancellationToken: ct
            )
        );
        if (inTestSplit)
            throw new InvalidQueryException(
                $"'{text}' is in the test split - labelling it would leak test data into training."
            );

        // A train row with no confirmed_intent_id is original dataset data, not an earlier
        // promotion - never overwrite it.
        if (logged.ConfirmedIntentId is null)
        {
            var inTrainSplit = await conn.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    LabelledQueriesSchema.Sql.ExistsBySplitAndText,
                    new { Split = "train", QueryText = text.Value },
                    tx,
                    cancellationToken: ct
                )
            );
            if (inTrainSplit)
                throw new InvalidQueryException($"'{text}' is already labelled in the dataset.");
        }

        await conn.ExecuteAsync(
            new CommandDefinition(
                LabelledQueriesSchema.Sql.UpsertTrainExample,
                new
                {
                    QueryText = text.Value,
                    IntentId = intentId.Value,
                    Embedding = logged.Embedding,
                },
                tx,
                cancellationToken: ct
            )
        );
        var confirmed = await conn.ExecuteAsync(
            new CommandDefinition(
                ClassifiedQueriesSchema.Sql.ConfirmPromotion,
                new { QueryText = text.Value, IntentId = intentId.Value },
                tx,
                cancellationToken: ct
            )
        );
        if (confirmed != 1)
            throw new InvalidOperationException(
                $"Expected to confirm exactly one classified_queries row for '{text}', affected {confirmed}."
            );

        await tx.CommitAsync(ct);
    }
}

internal static class ClassifiedQueryRowExtensions
{
    public static ClassifiedQuery MapToClassifiedQuery(this ClassifiedQueryRow row) =>
        new(
            row.Embedding.ToEmbedding(),
            row.ConfirmedIntentId is null ? null : IntentId.Create(row.ConfirmedIntentId)
        );
}
