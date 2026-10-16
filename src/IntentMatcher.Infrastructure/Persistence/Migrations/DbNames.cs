namespace IntentMatcher.Infrastructure.Persistence.Migrations;

// Names used by migrations. Migrations keep their own constants (not the *Schema classes) so an
// applied migration never changes when a row property is renamed later.
internal static class DbNames
{
    public const string VectorType = "vector(1536)"; // text-embedding-3-small

    public static class Intents
    {
        public const string Table = "intents";
        public const string Id = "intent_id";
    }

    public static class LabelledQueries
    {
        public const string Table = "labelled_queries";
        public const string Id = "query_id";
    }
}
