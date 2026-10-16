namespace IntentMatcher.Infrastructure.Persistence.Migrations;

using FluentMigrator;

// One row per query: labelled intent, best and runner-up intent with scores, the margin between them
// and whether the best match is correct. Read model for reporting.
[Migration(5, "query_match_report view")]
public sealed class M005_CreateQueryMatchReportView : Migration
{
    public override void Up() =>
        Execute.Sql(
            """
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
            """
        );

    public override void Down() => Execute.Sql("DROP VIEW IF EXISTS query_match_report;");
}
