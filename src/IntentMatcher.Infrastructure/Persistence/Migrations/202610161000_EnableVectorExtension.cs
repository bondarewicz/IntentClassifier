namespace IntentMatcher.Infrastructure.Persistence.Migrations;

using FluentMigrator;

[Migration(202610161000, "Enable pgvector extension")]
public sealed class EnableVectorExtension : Migration
{
    public override void Up() => Execute.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

    public override void Down() => Execute.Sql("DROP EXTENSION IF EXISTS vector;");
}
