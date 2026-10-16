namespace IntentMatcher.Infrastructure.Persistence.Migrations;

using FluentMigrator;

// Banking77 customer queries with their labelled intent. Keyed by (split, query_text): a handful of
// texts appear in both train and test and each split keeps its own row.
[Migration(3, "labelled_queries")]
public sealed class M003_CreateLabelledQueries : Migration
{
    public override void Up()
    {
        Create
            .Table(DbNames.LabelledQueries.Table)
            .WithColumn(DbNames.LabelledQueries.Id)
            .AsGuid()
            .NotNullable()
            .PrimaryKey("pk_labelled_queries")
            .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))
            .WithColumn("split")
            .AsString(8)
            .NotNullable()
            .WithColumn("query_text")
            .AsString(int.MaxValue)
            .NotNullable()
            .WithColumn("intent_id")
            .AsString(int.MaxValue)
            .NotNullable()
            .ForeignKey("fk_labelled_queries_intents", DbNames.Intents.Table, DbNames.Intents.Id)
            .WithColumn("embedding")
            .AsCustom(DbNames.VectorType)
            .Nullable()
            .WithColumn("embedded_at")
            .AsDateTime()
            .Nullable();

        Execute.Sql(
            $"ALTER TABLE {DbNames.LabelledQueries.Table} ADD CONSTRAINT ck_labelled_queries_split CHECK (split IN ('train', 'test'));"
        );
        Create
            .UniqueConstraint("ux_labelled_queries_split_text")
            .OnTable(DbNames.LabelledQueries.Table)
            .Columns("split", "query_text");
        Create
            .Index("ix_labelled_queries_query_text")
            .OnTable(DbNames.LabelledQueries.Table)
            .OnColumn("query_text");
        Create
            .Index("ix_labelled_queries_intent_id")
            .OnTable(DbNames.LabelledQueries.Table)
            .OnColumn("intent_id");
    }

    public override void Down() => Delete.Table(DbNames.LabelledQueries.Table);
}
