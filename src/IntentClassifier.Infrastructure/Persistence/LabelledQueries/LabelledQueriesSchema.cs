namespace IntentClassifier.Infrastructure.Persistence.LabelledQueries;

internal static class LabelledQueriesSchema
{
    public const string Table = "labelled_queries";

    public static class Sql
    {
        // Prefer a row that already has an embedding when the text exists in both splits.
        public static readonly string SelectByText = $"""
            SELECT query_id, split, query_text, intent_id, embedding
            FROM {Table}
            WHERE query_text = @QueryText
            ORDER BY embedding IS NULL
            LIMIT 1
            """;

        // Used by ClassifiedQueryLog.Confirm (not a repository) to guard a label promotion
        // against train/test leakage and against overwriting original dataset labels.
        public static readonly string ExistsBySplitAndText = $"""
            SELECT EXISTS (
                SELECT 1 FROM {Table} WHERE split = @Split AND query_text = @QueryText
            )
            """;

        // Owned here, not ClassifiedQueriesSchema, because it writes this table; Confirm runs
        // it inside its own transaction alongside the classified_queries update.
        public static readonly string UpsertTrainExample = $"""
            INSERT INTO {Table} (split, query_text, intent_id, embedding, embedded_at)
            VALUES ('train', @QueryText, @IntentId, @Embedding, now() AT TIME ZONE 'utc')
            ON CONFLICT (split, query_text) DO UPDATE SET
                intent_id = EXCLUDED.intent_id,
                embedding = EXCLUDED.embedding,
                embedded_at = EXCLUDED.embedded_at
            """;

        public static readonly string SelectWithoutEmbedding = $"""
            SELECT query_id, split, query_text, intent_id
            FROM {Table}
            WHERE embedding IS NULL
            ORDER BY split, query_text
            """;

        // One statement per batch: ids and vectors as two parallel arrays, unnested and joined.
        public static readonly string UpdateEmbeddings = $"""
            UPDATE {Table} q
            SET embedding = u.embedding::vector,
                embedded_at = now() AT TIME ZONE 'utc'
            FROM unnest(@QueryIds, @Embeddings) AS u(query_id, embedding)
            WHERE q.query_id = u.query_id
            """;

        public static readonly string InsertMissing = $"""
            INSERT INTO {Table} (split, query_text, intent_id)
            SELECT * FROM unnest(@Splits, @QueryTexts, @IntentIds)
            ON CONFLICT (split, query_text) DO NOTHING
            """;

        public static readonly string CountBySplit = $"""
            SELECT split, count(*) FROM {Table} GROUP BY split
            """;
    }
}
