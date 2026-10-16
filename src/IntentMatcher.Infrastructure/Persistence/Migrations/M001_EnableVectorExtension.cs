namespace IntentMatcher.Infrastructure.Persistence.Migrations;

using FluentMigrator;

[Migration(1, "pgvector extension")]
public sealed class M001_EnableVectorExtension : Migration
{
    public override void Up() => Execute.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

    public override void Down() => Execute.Sql("DROP EXTENSION IF EXISTS vector;");
}
