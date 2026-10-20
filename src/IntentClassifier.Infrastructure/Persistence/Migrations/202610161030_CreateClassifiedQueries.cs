namespace IntentClassifier.Infrastructure.Persistence.Migrations;

using FluentMigrator;

// One row per normalized query text ever classified. The embedding is written once on first
// classify and never overwritten; times_classified/last_classified_at/best match/ambiguous
// refresh on every repeat (see ClassifiedQueryLog.Record). confirmed_intent_id/confirmed_at
// record a human's `label` promotion into labelled_queries, if any.
[Migration(202610161030, "Create classified_queries table")]
public sealed class CreateClassifiedQueries : Migration
{
    public override void Up()
    {
        Create
            .Table(DbNames.ClassifiedQueries.Table)
            .WithColumn(DbNames.ClassifiedQueries.Id)
            .AsGuid()
            .NotNullable()
            .PrimaryKey("pk_classified_queries")
            .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))
            .WithColumn("query_text")
            .AsString(int.MaxValue)
            .NotNullable()
            .WithColumn("embedding")
            .AsCustom(DbNames.VectorType)
            .NotNullable()
            .WithColumn("representation")
            .AsString(8)
            .NotNullable()
            .WithColumn("best_intent_id")
            .AsString(int.MaxValue)
            .Nullable()
            .ForeignKey(
                "fk_classified_queries_best_intent",
                DbNames.Intents.Table,
                DbNames.Intents.Id
            )
            .WithColumn("best_similarity")
            .AsDouble()
            .Nullable()
            .WithColumn("ambiguous")
            .AsBoolean()
            .NotNullable()
            .WithColumn("times_classified")
            .AsInt32()
            .NotNullable()
            .WithDefaultValue(1)
            .WithColumn("first_classified_at")
            .AsDateTime()
            .NotNullable()
            .WithColumn("last_classified_at")
            .AsDateTime()
            .NotNullable()
            .WithColumn("confirmed_intent_id")
            .AsString(int.MaxValue)
            .Nullable()
            .ForeignKey(
                "fk_classified_queries_confirmed_intent",
                DbNames.Intents.Table,
                DbNames.Intents.Id
            )
            .WithColumn("confirmed_at")
            .AsDateTime()
            .Nullable();

        Create
            .UniqueConstraint("ux_classified_queries_query_text")
            .OnTable(DbNames.ClassifiedQueries.Table)
            .Columns("query_text");
    }

    public override void Down() => Delete.Table(DbNames.ClassifiedQueries.Table);
}
