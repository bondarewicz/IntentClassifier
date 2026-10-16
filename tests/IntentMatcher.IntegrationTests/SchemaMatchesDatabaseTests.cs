namespace IntentMatcher.IntegrationTests;

using System.Reflection;
using Dapper;
using IntentMatcher.Infrastructure.Persistence.ClassifiedQueries;
using IntentMatcher.Infrastructure.Persistence.IntentMatches;
using IntentMatcher.Infrastructure.Persistence.Intents;
using IntentMatcher.Infrastructure.Persistence.LabelledQueries;
using IntentMatcher.Infrastructure.Persistence.Reports;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

// Nothing else links a Schema.Columns constant (derived from a Row property name via ToSnakeCase)
// to the migration that actually creates the column it names; this is that link. A Row property
// rename with no matching migration change fails here instead of in whichever SQL happens to run
// first.
public sealed class SchemaMatchesDatabaseTests(PostgresFixture db) : IClassFixture<PostgresFixture>
{
    [Theory]
    [MemberData(nameof(SchemaTables))]
    public async Task Schema_columns_exist_in_the_database(string table, Type columnsType)
    {
        var dataSource = db.Services!.GetRequiredService<NpgsqlDataSource>();
        await using var conn = await dataSource.OpenConnectionAsync();
        var actual = (
            await conn.QueryAsync<string>(
                "SELECT column_name FROM information_schema.columns WHERE table_name = @Table",
                new { Table = table }
            )
        ).ToHashSet();

        // Excludes LabelledQueriesSchema.Columns.All: a comma-joined SQL fragment, not a column name.
        var expected = columnsType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.Name != "All")
            .Select(f => (string)f.GetValue(null)!);

        Assert.All(expected, column => Assert.Contains(column, actual));
    }

    public static IEnumerable<object[]> SchemaTables()
    {
        yield return [IntentsSchema.Table, typeof(IntentsSchema.Columns)];
        yield return [LabelledQueriesSchema.Table, typeof(LabelledQueriesSchema.Columns)];
        yield return [QueryIntentMatchesSchema.Table, typeof(QueryIntentMatchesSchema.Columns)];
        yield return [QueryMatchReportSchema.View, typeof(QueryMatchReportSchema.Columns)];
        yield return [ClassifiedQueriesSchema.Table, typeof(ClassifiedQueriesSchema.Columns)];
    }
}
