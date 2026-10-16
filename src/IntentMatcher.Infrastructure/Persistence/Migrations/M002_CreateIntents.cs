namespace IntentMatcher.Infrastructure.Persistence.Migrations;

using FluentMigrator;

// The 77 Banking77 intents. Two vector representations: the embedded readable name and the
// centroid (mean) of the intent's embedded train queries.
[Migration(2, "intents")]
public sealed class M002_CreateIntents : Migration
{
    public override void Up() =>
        Create
            .Table(DbNames.Intents.Table)
            .WithColumn(DbNames.Intents.Id)
            .AsString(int.MaxValue)
            .NotNullable()
            .PrimaryKey("pk_intents")
            .WithColumn("intent_name")
            .AsString(int.MaxValue)
            .NotNullable()
            .WithColumn("name_embedding")
            .AsCustom(DbNames.VectorType)
            .Nullable()
            .WithColumn("centroid_embedding")
            .AsCustom(DbNames.VectorType)
            .Nullable()
            .WithColumn("example_count")
            .AsInt32()
            .NotNullable()
            .WithDefaultValue(0)
            .WithColumn("created_at")
            .AsDateTime()
            .NotNullable()
            .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("centroid_updated_at")
            .AsDateTime()
            .Nullable();

    public override void Down() => Delete.Table(DbNames.Intents.Table);
}
