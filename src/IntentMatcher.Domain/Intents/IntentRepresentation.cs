namespace IntentMatcher.Domain.Intents;

// How an intent is represented in vector space when matching a query against it.
public enum IntentRepresentation
{
    // Mean embedding of the intent's train queries (prototype). Default: far more accurate.
    Centroid,

    // Embedding of the intent's readable name only.
    Name
}
