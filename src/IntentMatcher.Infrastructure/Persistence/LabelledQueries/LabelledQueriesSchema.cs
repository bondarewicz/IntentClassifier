namespace IntentMatcher.Infrastructure.Persistence.LabelledQueries;

internal static class LabelledQueriesSchema
{
    public const string Table = "labelled_queries";

    public static class Columns
    {
        public static readonly string QueryId = nameof(LabelledQueryRow.QueryId).ToSnakeCase();
        public static readonly string Split = nameof(LabelledQueryRow.Split).ToSnakeCase();
        public static readonly string QueryText = nameof(LabelledQueryRow.QueryText).ToSnakeCase();
        public static readonly string IntentId = nameof(LabelledQueryRow.IntentId).ToSnakeCase();
        public static readonly string Embedding = nameof(LabelledQueryRow.Embedding).ToSnakeCase();
        public static readonly string EmbeddedAt = nameof(LabelledQueryRow.EmbeddedAt)
            .ToSnakeCase();

        public static readonly string All = $"{QueryId}, {Split}, {QueryText}, {IntentId}";
    }

    public static class Sql
    {
        // Prefer a row that already has an embedding when the text exists in both splits.
        public static readonly string SelectByText = $"""
            SELECT {Columns.All}, {Columns.Embedding}
            FROM {Table}
            WHERE {Columns.QueryText} = @QueryText
            ORDER BY {Columns.Embedding} IS NULL
            LIMIT 1
            """;

        public static readonly string SelectWithoutEmbedding = $"""
            SELECT {Columns.All}
            FROM {Table}
            WHERE {Columns.Embedding} IS NULL
            ORDER BY {Columns.Split}, {Columns.QueryText}
            """;

        // One statement per batch: ids and vectors as two parallel arrays, unnested and joined.
        public static readonly string UpdateEmbeddings = $"""
            UPDATE {Table} q
            SET {Columns.Embedding} = u.embedding::vector,
                {Columns.EmbeddedAt} = now() AT TIME ZONE 'utc'
            FROM unnest(@QueryIds, @Embeddings) AS u(query_id, embedding)
            WHERE q.{Columns.QueryId} = u.query_id
            """;

        public static readonly string InsertMissing = $"""
            INSERT INTO {Table} ({Columns.Split}, {Columns.QueryText}, {Columns.IntentId})
            SELECT * FROM unnest(@Splits, @QueryTexts, @IntentIds)
            ON CONFLICT ({Columns.Split}, {Columns.QueryText}) DO NOTHING
            """;

        public static readonly string CountBySplit = $"""
            SELECT {Columns.Split}, count(*) FROM {Table} GROUP BY {Columns.Split}
            """;
    }
}
