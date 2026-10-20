namespace IntentClassifier.Infrastructure.Persistence.Migrations;

using FluentMigrator;

// similarity was float4 (CreateQueryIntentMatches) while the ambiguity margin it feeds is
// compared in float8/double everywhere else (the view, evaluate, the C# path), so the same
// margin could classify a query differently depending on which path computed it. Widens the
// stored column to match.
[Migration(202610161025, "Widen query_intent_matches.similarity to double precision")]
public sealed class WidenMatchSimilarity : Migration
{
    private const string Table = DbNames.QueryIntentMatches.Table;
    private const string View = "query_match_report";

    // Must match CreateQueryMatchReportView's view definition exactly - Postgres refuses to
    // retype a column a view depends on, so the view is dropped and recreated verbatim
    // around the ALTER.
    private const string CreateView = """
        CREATE VIEW query_match_report AS
        SELECT q.query_id,
               q.split,
               q.query_text,
               q.intent_id                   AS labelled_intent_id,
               b.intent_id                   AS best_intent_id,
               ib.intent_name                AS best_intent_name,
               b.similarity                  AS best_similarity,
               r.intent_id                   AS runner_up_intent_id,
               ir.intent_name                AS runner_up_intent_name,
               r.similarity                  AS runner_up_similarity,
               b.similarity - r.similarity   AS margin,
               b.intent_id = q.intent_id     AS is_correct
        FROM labelled_queries q
        JOIN query_intent_matches b      ON b.query_id = q.query_id AND b.rank = 1
        JOIN intents ib                  ON ib.intent_id = b.intent_id
        LEFT JOIN query_intent_matches r ON r.query_id = q.query_id AND r.rank = 2
        LEFT JOIN intents ir             ON ir.intent_id = r.intent_id;
        """;

    public override void Up()
    {
        Execute.Sql($"DROP VIEW {View};");
        Alter.Column("similarity").OnTable(Table).AsDouble().NotNullable();
        Execute.Sql(CreateView);
    }

    public override void Down()
    {
        Execute.Sql($"DROP VIEW {View};");
        Alter.Column("similarity").OnTable(Table).AsFloat().NotNullable();
        Execute.Sql(CreateView);
    }
}
