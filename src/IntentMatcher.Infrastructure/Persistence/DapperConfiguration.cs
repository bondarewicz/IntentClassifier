namespace IntentMatcher.Infrastructure.Persistence;

using Dapper;
using Pgvector.Dapper;

internal static class DapperConfiguration
{
    private static readonly Lazy<bool> Registration = new(() =>
    {
        SqlMapper.AddTypeHandler(new VectorTypeHandler());
        // Rows use PascalCase properties for snake_case columns (intent_id -> IntentId).
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        return true;
    });

    // Dapper configuration is process-wide; register once.
    public static void EnsureRegistered() => _ = Registration.Value;
}
