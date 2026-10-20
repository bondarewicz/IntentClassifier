namespace IntentClassifier.Infrastructure.Persistence.Migrations;

using System.Data;
using FluentMigrator;

// Top-K intents per query from the last matching run. Narrow rows keyed by (query_id, rank) so the
// report reads "best match per query" from the PK prefix and "queries per intent" from the partial index.
[Migration(202610161015, "Create query_intent_matches table")]
public sealed class CreateQueryIntentMatches : Migration
{
    private const string Table = DbNames.QueryIntentMatches.Table;

    public override void Up()
    {
        Create
            .Table(Table)
            .WithColumn("query_id")
            .AsGuid()
            .NotNullable()
            .ForeignKey(
                "fk_query_intent_matches_queries",
                DbNames.LabelledQueries.Table,
                DbNames.LabelledQueries.Id
            )
            .OnDelete(Rule.Cascade)
            .WithColumn("rank")
            .AsInt16()
            .NotNullable()
            .WithColumn("intent_id")
            .AsString(int.MaxValue)
            .NotNullable()
            .ForeignKey(
                "fk_query_intent_matches_intents",
                DbNames.Intents.Table,
                DbNames.Intents.Id
            )
            .OnDelete(Rule.Cascade)
            .WithColumn("similarity")
            .AsFloat()
            .NotNullable();

        Create.PrimaryKey("pk_query_intent_matches").OnTable(Table).Columns("query_id", "rank");
        Execute.Sql(
            $"CREATE INDEX ix_query_intent_matches_best_by_intent ON {Table} (intent_id, similarity DESC) WHERE rank = 1;"
        );
    }

    public override void Down() => Delete.Table(Table);
}
