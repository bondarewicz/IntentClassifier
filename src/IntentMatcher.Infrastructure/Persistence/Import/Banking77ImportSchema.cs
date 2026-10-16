namespace IntentMatcher.Infrastructure.Persistence.Import;

using IntentMatcher.Infrastructure.Persistence.Intents;
using IntentMatcher.Infrastructure.Persistence.LabelledQueries;

// Staging for the Banking77 CSVs (text,category): Postgres parses the CSV via COPY, the rows are then
// normalized and validated by the domain types before being inserted.
internal static class Banking77ImportSchema
{
    public const string Stage = "banking77_stage";

    public static class Sql
    {
        public static readonly string CreateStage = $"""
            CREATE TEMP TABLE {Stage} (query_text text, category text, split text) ON COMMIT DROP
            """;

        public static readonly string CopyIntoStage =
            $"COPY {Stage} (query_text, category) FROM STDIN (FORMAT csv, HEADER true)";

        // Tags whichever rows were COPYed most recently; relies on CopyIntoStage marking
        // immediately after each COPY - copying both CSVs before marking would label every row
        // with the split marked last.
        public static readonly string MarkSplit =
            $"UPDATE {Stage} SET split = @Split WHERE split IS NULL";

        public static readonly string SelectStaged = $"""
            SELECT query_text AS {nameof(StagedRow.QueryText)}, category AS {nameof(
                StagedRow.Category
            )}, split AS {nameof(StagedRow.Split)}
            FROM {Stage}
            """;

        public static readonly string AnalyzeImported =
            $"ANALYZE {IntentsSchema.Table}; ANALYZE {LabelledQueriesSchema.Table};";
    }

    internal sealed class StagedRow
    {
        public string QueryText { get; set; } = "";
        public string Category { get; set; } = "";
        public string Split { get; set; } = "";
    }
}
