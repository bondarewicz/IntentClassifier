namespace IntentMatcher.Infrastructure.Persistence.IntentMatches;

using Dapper;
using IntentMatcher.Application.Matching;
using IntentMatcher.Domain.Intents;
using Npgsql;

internal sealed class IntentMatchProjection(NpgsqlDataSource dataSource) : IIntentMatchProjection
{
    // The new results replace the previous run atomically.
    public async Task<long> RebuildAll(
        int top,
        IntentRepresentation representation,
        CancellationToken ct = default
    )
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await conn.ExecuteAsync(
            new CommandDefinition(
                QueryIntentMatchesSchema.Sql.DeleteAll,
                transaction: tx,
                cancellationToken: ct
            )
        );
        await conn.ExecuteAsync(
            new CommandDefinition(
                QueryIntentMatchesSchema.Sql.InsertTopMatches(representation),
                new { Top = top },
                tx,
                commandTimeout: 0,
                cancellationToken: ct
            )
        );
        var stored = await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(
                QueryIntentMatchesSchema.Sql.Count,
                transaction: tx,
                cancellationToken: ct
            )
        );

        await tx.CommitAsync(ct);
        return stored;
    }
}
