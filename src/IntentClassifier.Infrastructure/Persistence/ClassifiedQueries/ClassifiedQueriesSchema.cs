namespace IntentClassifier.Infrastructure.Persistence.ClassifiedQueries;

internal static class ClassifiedQueriesSchema
{
    public const string Table = "classified_queries";

    public static class Sql
    {
        public static readonly string SelectEmbeddingByText = $"""
            SELECT embedding FROM {Table} WHERE query_text = @QueryText
            """;

        // Locks the row for the rest of Confirm's transaction, so a concurrent classify (whose
        // upsert would otherwise block on the same row anyway) or a second label call cannot
        // race the guard checks against the write.
        public static readonly string SelectForConfirmUpdate = $"""
            SELECT embedding, confirmed_intent_id FROM {Table} WHERE query_text = @QueryText FOR UPDATE
            """;

        // Embedding is only ever set on insert; a repeat only refreshes the rest.
        public static readonly string Upsert = $"""
            INSERT INTO {Table} (
                query_text, embedding, representation, best_intent_id, best_similarity, ambiguous,
                first_classified_at, last_classified_at
            )
            VALUES (
                @QueryText, @Embedding, @Representation, @BestIntentId, @BestSimilarity, @Ambiguous,
                now() AT TIME ZONE 'utc', now() AT TIME ZONE 'utc'
            )
            ON CONFLICT (query_text) DO UPDATE SET
                representation = EXCLUDED.representation,
                best_intent_id = EXCLUDED.best_intent_id,
                best_similarity = EXCLUDED.best_similarity,
                ambiguous = EXCLUDED.ambiguous,
                times_classified = {Table}.times_classified + 1,
                last_classified_at = EXCLUDED.last_classified_at
            """;

        public static readonly string ConfirmPromotion = $"""
            UPDATE {Table}
            SET confirmed_intent_id = @IntentId, confirmed_at = now() AT TIME ZONE 'utc'
            WHERE query_text = @QueryText
            """;
    }
}
