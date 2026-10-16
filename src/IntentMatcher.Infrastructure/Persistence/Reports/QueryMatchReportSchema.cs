namespace IntentMatcher.Infrastructure.Persistence.Reports;

using IntentMatcher.Domain.Intents;
using IntentMatcher.Infrastructure.Persistence.Intents;
using IntentMatcher.Infrastructure.Persistence.LabelledQueries;

internal static class QueryMatchReportSchema
{
    public const string View = "query_match_report";

    public static class Columns
    {
        public static readonly string Split = nameof(QueryMatchReportRow.Split).ToSnakeCase();
        public static readonly string BestSimilarity = nameof(QueryMatchReportRow.BestSimilarity)
            .ToSnakeCase();
        public static readonly string Margin = nameof(QueryMatchReportRow.Margin).ToSnakeCase();
        public static readonly string IsCorrect = nameof(QueryMatchReportRow.IsCorrect)
            .ToSnakeCase();
    }

    public static class Sql
    {
        // Train before test, explicitly - not relying on the alphabetical sort of the split strings.
        public static readonly string SelectSummaryBySplit = $"""
            SELECT {Columns.Split}                                                      AS {nameof(
                    SplitSummaryRow.Split
                ).ToSnakeCase()},
                   count(*)                                                             AS {nameof(
                    SplitSummaryRow.Queries
                ).ToSnakeCase()},
                   coalesce(avg({Columns.BestSimilarity}), 0)::double precision         AS {nameof(
                    SplitSummaryRow.AverageBestSimilarity
                ).ToSnakeCase()},
                   count(*) FILTER (WHERE {Columns.IsCorrect})                          AS {nameof(
                    SplitSummaryRow.Correct
                ).ToSnakeCase()},
                   count(*) FILTER (WHERE {Columns.Margin} < @Margin)                   AS {nameof(
                    SplitSummaryRow.Ambiguous
                ).ToSnakeCase()}
            FROM {View}
            GROUP BY {Columns.Split}
            ORDER BY ({Columns.Split} = 'train') DESC
            """;

        public static readonly string EvaluateByCentroid = Evaluate(IntentRepresentation.Centroid);
        public static readonly string EvaluateByName = Evaluate(IntentRepresentation.Name);

        public static string EvaluateFor(IntentRepresentation representation) =>
            representation switch
            {
                IntentRepresentation.Centroid => EvaluateByCentroid,
                IntentRepresentation.Name => EvaluateByName,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(representation),
                    representation,
                    null
                ),
            };

        // Classifies every embedded query of a split against the intents on the fly (top @Top) and
        // compares with the labelled intent; nothing is stored.
        private static string Evaluate(IntentRepresentation representation)
        {
            var intentEmbedding = $"i.{IntentsSchema.Columns.EmbeddingFor(representation)}";
            var distance = $"{intentEmbedding} <=> q.{LabelledQueriesSchema.Columns.Embedding}";
            return $"""
                WITH ranked AS (
                    SELECT q.{LabelledQueriesSchema.Columns.QueryId} AS query_id,
                           q.{LabelledQueriesSchema.Columns.IntentId} AS expected,
                           m.intent_id, m.rank, m.similarity
                    FROM {LabelledQueriesSchema.Table} q
                    CROSS JOIN LATERAL (
                        SELECT i.{IntentsSchema.Columns.IntentId} AS intent_id,
                               1 - ({distance}) AS similarity,
                               row_number() OVER (ORDER BY {distance}) AS rank
                        FROM {IntentsSchema.Table} i
                        WHERE {intentEmbedding} IS NOT NULL
                        ORDER BY {distance}
                        LIMIT @Top
                    ) m
                    WHERE q.{LabelledQueriesSchema.Columns.Split} = @Split
                      AND q.{LabelledQueriesSchema.Columns.Embedding} IS NOT NULL
                ),
                per_query AS (
                    SELECT query_id,
                           bool_or(rank = 1 AND intent_id = expected) AS top1,
                           bool_or(intent_id = expected)              AS top3,
                           coalesce(max(similarity) FILTER (WHERE rank = 1) - max(similarity) FILTER (WHERE rank = 2) < @Margin, false) AS ambiguous
                    FROM ranked
                    GROUP BY query_id
                )
                SELECT count(*)                                  AS {nameof(
                        EvaluationRow.Queries
                    ).ToSnakeCase()},
                       count(*) FILTER (WHERE top1)              AS {nameof(
                        EvaluationRow.Top1Correct
                    ).ToSnakeCase()},
                       count(*) FILTER (WHERE top3)              AS {nameof(
                        EvaluationRow.Top3Correct
                    ).ToSnakeCase()},
                       count(*) FILTER (WHERE ambiguous)         AS {nameof(
                        EvaluationRow.Ambiguous
                    ).ToSnakeCase()},
                       count(*) FILTER (WHERE ambiguous AND top1) AS {nameof(
                        EvaluationRow.AmbiguousTop1Correct
                    ).ToSnakeCase()}
                FROM per_query
                """;
        }
    }
}
