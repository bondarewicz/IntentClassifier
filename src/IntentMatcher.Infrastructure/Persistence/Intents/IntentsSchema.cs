namespace IntentMatcher.Infrastructure.Persistence.Intents;

using IntentMatcher.Domain.Intents;
using IntentMatcher.Infrastructure.Persistence.LabelledQueries;

internal static class IntentsSchema
{
    public const string Table = "intents";

    public static class Columns
    {
        public static readonly string IntentId = nameof(IntentRow.IntentId).ToSnakeCase();
        public static readonly string IntentName = nameof(IntentRow.IntentName).ToSnakeCase();
        public static readonly string NameEmbedding = nameof(IntentRow.NameEmbedding).ToSnakeCase();
        public static readonly string CentroidEmbedding = nameof(IntentRow.CentroidEmbedding)
            .ToSnakeCase();
        public static readonly string ExampleCount = nameof(IntentRow.ExampleCount).ToSnakeCase();
        public static readonly string CentroidUpdatedAt = nameof(IntentRow.CentroidUpdatedAt)
            .ToSnakeCase();

        public static string EmbeddingFor(IntentRepresentation representation) =>
            representation switch
            {
                IntentRepresentation.Centroid => CentroidEmbedding,
                IntentRepresentation.Name => NameEmbedding,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(representation),
                    representation,
                    null
                ),
            };
    }

    public static class Sql
    {
        public static readonly string SelectWithoutNameEmbedding = $"""
            SELECT {Columns.IntentId}, {Columns.IntentName}
            FROM {Table}
            WHERE {Columns.NameEmbedding} IS NULL
            ORDER BY {Columns.IntentId}
            """;

        public static readonly string UpdateNameEmbeddings = $"""
            UPDATE {Table} i
            SET {Columns.NameEmbedding} = u.embedding::vector
            FROM unnest(@IntentIds, @Embeddings) AS u(intent_id, embedding)
            WHERE i.{Columns.IntentId} = u.intent_id
            """;

        public static readonly string InsertMissing = $"""
            INSERT INTO {Table} ({Columns.IntentId}, {Columns.IntentName})
            SELECT * FROM unnest(@IntentIds, @IntentNames)
            ON CONFLICT ({Columns.IntentId}) DO NOTHING
            """;

        // Centroid = mean of the intent's embedded train queries (pgvector's avg(vector)).
        public static readonly string UpdateCentroids = $"""
            UPDATE {Table} i
            SET {Columns.CentroidEmbedding} = c.centroid,
                {Columns.ExampleCount} = c.examples,
                {Columns.CentroidUpdatedAt} = now() AT TIME ZONE 'utc'
            FROM (
                SELECT {LabelledQueriesSchema.Columns.IntentId} AS intent_id,
                       avg({LabelledQueriesSchema.Columns.Embedding}) AS centroid,
                       count(*) AS examples
                FROM {LabelledQueriesSchema.Table}
                WHERE {LabelledQueriesSchema.Columns.Split} = 'train'
                  AND {LabelledQueriesSchema.Columns.Embedding} IS NOT NULL
                GROUP BY {LabelledQueriesSchema.Columns.IntentId}
            ) c
            WHERE i.{Columns.IntentId} = c.intent_id
            """;

        public static readonly string SelectClosestByCentroid = SelectClosest(
            Columns.CentroidEmbedding
        );
        public static readonly string SelectClosestByName = SelectClosest(Columns.NameEmbedding);

        public static string SelectClosestFor(IntentRepresentation representation) =>
            representation switch
            {
                IntentRepresentation.Centroid => SelectClosestByCentroid,
                IntentRepresentation.Name => SelectClosestByName,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(representation),
                    representation,
                    null
                ),
            };

        // Exact search with pgvector's cosine distance operator; similarity = 1 - distance.
        private static string SelectClosest(string embeddingColumn)
        {
            var distance = $"{embeddingColumn} <=> @Embedding";
            return $"""
                SELECT row_number() OVER (ORDER BY {distance})::int AS {nameof(
                        IntentMatchRow.Rank
                    ).ToSnakeCase()},
                       {Columns.IntentId},
                       {Columns.IntentName},
                       1 - ({distance}) AS {nameof(IntentMatchRow.Similarity).ToSnakeCase()}
                FROM {Table}
                WHERE {embeddingColumn} IS NOT NULL
                ORDER BY {distance}
                LIMIT @Top
                """;
        }
    }
}
