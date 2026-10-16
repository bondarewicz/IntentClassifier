namespace IntentMatcher.Infrastructure.Persistence.ClassifiedQueries;

internal static class ClassifiedQueriesSchema
{
    public const string Table = "classified_queries";

    public static class Columns
    {
        public static readonly string ClassifiedQueryId = nameof(
                ClassifiedQueryRow.ClassifiedQueryId
            )
            .ToSnakeCase();
        public static readonly string QueryText = nameof(ClassifiedQueryRow.QueryText)
            .ToSnakeCase();
        public static readonly string Embedding = nameof(ClassifiedQueryRow.Embedding)
            .ToSnakeCase();
        public static readonly string Representation = nameof(ClassifiedQueryRow.Representation)
            .ToSnakeCase();
        public static readonly string BestIntentId = nameof(ClassifiedQueryRow.BestIntentId)
            .ToSnakeCase();
        public static readonly string BestSimilarity = nameof(ClassifiedQueryRow.BestSimilarity)
            .ToSnakeCase();
        public static readonly string Ambiguous = nameof(ClassifiedQueryRow.Ambiguous)
            .ToSnakeCase();
        public static readonly string TimesClassified = nameof(ClassifiedQueryRow.TimesClassified)
            .ToSnakeCase();
        public static readonly string FirstClassifiedAt = nameof(
                ClassifiedQueryRow.FirstClassifiedAt
            )
            .ToSnakeCase();
        public static readonly string LastClassifiedAt = nameof(ClassifiedQueryRow.LastClassifiedAt)
            .ToSnakeCase();
        public static readonly string ConfirmedIntentId = nameof(
                ClassifiedQueryRow.ConfirmedIntentId
            )
            .ToSnakeCase();
        public static readonly string ConfirmedAt = nameof(ClassifiedQueryRow.ConfirmedAt)
            .ToSnakeCase();
    }

    public static class Sql
    {
        public static readonly string SelectByText = $"""
            SELECT {Columns.Embedding}, {Columns.ConfirmedIntentId}
            FROM {Table}
            WHERE {Columns.QueryText} = @QueryText
            """;

        // Locks the row for the rest of Confirm's transaction, so a concurrent classify (whose
        // upsert would otherwise block on the same row anyway) or a second label call cannot
        // race the guard checks against the write.
        public static readonly string SelectByTextForUpdate = $"""
            SELECT {Columns.Embedding}, {Columns.ConfirmedIntentId}
            FROM {Table}
            WHERE {Columns.QueryText} = @QueryText
            FOR UPDATE
            """;

        // Embedding is only ever set on insert; a repeat only refreshes the rest.
        public static readonly string Upsert = $"""
            INSERT INTO {Table} (
                {Columns.QueryText}, {Columns.Embedding}, {Columns.Representation},
                {Columns.BestIntentId}, {Columns.BestSimilarity}, {Columns.Ambiguous},
                {Columns.FirstClassifiedAt}, {Columns.LastClassifiedAt}
            )
            VALUES (
                @QueryText, @Embedding, @Representation,
                @BestIntentId, @BestSimilarity, @Ambiguous,
                now() AT TIME ZONE 'utc', now() AT TIME ZONE 'utc'
            )
            ON CONFLICT ({Columns.QueryText}) DO UPDATE SET
                {Columns.Representation} = EXCLUDED.{Columns.Representation},
                {Columns.BestIntentId} = EXCLUDED.{Columns.BestIntentId},
                {Columns.BestSimilarity} = EXCLUDED.{Columns.BestSimilarity},
                {Columns.Ambiguous} = EXCLUDED.{Columns.Ambiguous},
                {Columns.TimesClassified} = {Table}.{Columns.TimesClassified} + 1,
                {Columns.LastClassifiedAt} = EXCLUDED.{Columns.LastClassifiedAt}
            """;

        public static readonly string ConfirmPromotion = $"""
            UPDATE {Table}
            SET {Columns.ConfirmedIntentId} = @IntentId,
                {Columns.ConfirmedAt} = now() AT TIME ZONE 'utc'
            WHERE {Columns.QueryText} = @QueryText
            """;
    }
}
