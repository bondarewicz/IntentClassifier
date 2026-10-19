namespace IntentMatcher.Infrastructure.Persistence.Intents;

using Dapper;
using IntentMatcher.Domain.Embeddings;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Matching;
using Npgsql;

internal sealed class IntentRepository(NpgsqlDataSource dataSource) : IIntentRepository
{
    public async Task<IReadOnlyList<Intent>> ListWithoutNameEmbedding(
        CancellationToken ct = default
    )
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<IntentRow>(
            new CommandDefinition(
                IntentsSchema.Sql.SelectWithoutNameEmbedding,
                cancellationToken: ct
            )
        );
        return rows.Select(r => r.MapToIntent()).ToList();
    }

    public async Task SaveNameEmbeddings(
        IReadOnlyCollection<Intent> intents,
        CancellationToken ct = default
    )
    {
        var embedded = intents.Where(i => i.HasNameEmbedding).ToList();
        if (embedded.Count == 0)
            return;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            new CommandDefinition(
                IntentsSchema.Sql.UpdateNameEmbeddings,
                new
                {
                    IntentIds = embedded.Select(i => i.Id.Value).ToArray(),
                    Embeddings = embedded.Select(i => i.NameEmbedding!.ToVectorLiteral()).ToArray(),
                },
                cancellationToken: ct
            )
        );
    }

    public async Task<int> RecomputeCentroids(CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        return await conn.ExecuteAsync(
            new CommandDefinition(IntentsSchema.Sql.UpdateCentroids, cancellationToken: ct)
        );
    }

    public async Task<IReadOnlyList<IntentMatch>> FindClosest(
        Embedding embedding,
        int top,
        IntentRepresentation representation,
        CancellationToken ct = default
    )
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<IntentMatchRow>(
            new CommandDefinition(
                IntentsSchema.Sql.SelectClosest(representation),
                new { Embedding = embedding.ToVector(), Top = top },
                cancellationToken: ct
            )
        );
        return rows.Select(r => r.MapToIntentMatch()).ToList();
    }
}

internal static class IntentRowExtensions
{
    public static Intent MapToIntent(this IntentRow row) =>
        new(IntentId.Create(row.IntentId), row.IntentName, row.NameEmbedding?.ToEmbedding());

    public static IntentMatch MapToIntentMatch(this IntentMatchRow row) =>
        new(row.Rank, IntentId.Create(row.IntentId), row.IntentName, row.Similarity);
}
