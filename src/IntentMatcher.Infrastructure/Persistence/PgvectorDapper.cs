namespace IntentMatcher.Infrastructure.Persistence;

using System.Globalization;
using Dapper;
using IntentMatcher.Domain.Embeddings;
using IntentMatcher.Domain.Queries;
using Pgvector;
using Pgvector.Dapper;

internal static class PgvectorDapper
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

    public static Vector ToVector(this Embedding embedding) => new(embedding.Values);

    public static Embedding ToEmbedding(this Vector vector) => new(vector.Memory.Span);

    // pgvector text form "[0.1,0.2,...]", used to pass many vectors at once as text[]
    // (Dapper cannot bind an array of vectors directly).
    public static string ToVectorLiteral(this Embedding embedding)
    {
        var span = embedding.Values.Span;
        var parts = new string[span.Length];
        for (var i = 0; i < span.Length; i++)
            parts[i] = span[i].ToString("R", CultureInfo.InvariantCulture);
        return "[" + string.Join(',', parts) + "]";
    }

    public static string ToDb(this DatasetSplit split) =>
        split switch
        {
            DatasetSplit.Train => "train",
            DatasetSplit.Test => "test",
            _ => throw new ArgumentOutOfRangeException(nameof(split), split, null),
        };

    public static DatasetSplit ToDatasetSplit(this string split) =>
        split switch
        {
            "train" => DatasetSplit.Train,
            "test" => DatasetSplit.Test,
            _ => throw new ArgumentOutOfRangeException(nameof(split), split, null),
        };
}
