namespace IntentMatcher.Domain;

public sealed class InvalidQueryException(string message) : Exception(message);
