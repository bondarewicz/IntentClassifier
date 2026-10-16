namespace IntentMatcher.Infrastructure.Persistence.IntentMatches;

using IntentMatcher.Domain.Intents;
using IntentMatcher.Infrastructure.Persistence.Intents;
using IntentMatcher.Infrastructure.Persistence.LabelledQueries;

public static class QueryIntentMatchesSchema
{
    public static string Table => "query_intent_matches";

    public static class Columns
    {
        public static readonly string QueryId = nameof(QueryIntentMatchRow.QueryId).ToSnakeCase();
        public static readonly string Rank = nameof(QueryIntentMatchRow.Rank).ToSnakeCase();
        public static readonly string IntentId = nameof(QueryIntentMatchRow.IntentId).ToSnakeCase();
        public static readonly string Similarity = nameof(QueryIntentMatchRow.Similarity)
            .ToSnakeCase();
    }

    public static class Parameters
    {
        public const string Top = "@Top";
    }

    public static class Sql
    {
        public static readonly string DeleteAll = $"TRUNCATE {Table};";

        public static readonly string Count = $"SELECT count(*) FROM {Table}";

        public static readonly string InsertTopMatchesByCentroid = InsertTopMatches(
            IntentRepresentation.Centroid
        );
        public static readonly string InsertTopMatchesByName = InsertTopMatches(
            IntentRepresentation.Name
        );

        public static string InsertTopMatchesFor(IntentRepresentation representation) =>
            representation == IntentRepresentation.Centroid
                ? InsertTopMatchesByCentroid
                : InsertTopMatchesByName;

        // Set-based: pgvector computes queries x intents cosine distances in one statement.
        private static string InsertTopMatches(IntentRepresentation representation)
        {
            var intentEmbedding = $"i.{IntentsSchema.Columns.EmbeddingFor(representation)}";
            var distance = $"{intentEmbedding} <=> q.{LabelledQueriesSchema.Columns.Embedding}";
            return $"""
                INSERT INTO {Table} ({Columns.QueryId}, {Columns.Rank}, {Columns.IntentId}, {Columns.Similarity})
                SELECT q.{LabelledQueriesSchema.Columns.QueryId}, m.{Columns.Rank}, m.{Columns.IntentId}, m.{Columns.Similarity}
                FROM {LabelledQueriesSchema.Table} q
                CROSS JOIN LATERAL (
                    SELECT i.{IntentsSchema.Columns.IntentId} AS {Columns.IntentId},
                           (1 - ({distance}))::real AS {Columns.Similarity},
                           row_number() OVER (ORDER BY {distance})::smallint AS {Columns.Rank}
                    FROM {IntentsSchema.Table} i
                    WHERE {intentEmbedding} IS NOT NULL
                    ORDER BY {distance}
                    LIMIT {Parameters.Top}
                ) m
                WHERE q.{LabelledQueriesSchema.Columns.Embedding} IS NOT NULL;
                """;
        }
    }
}
