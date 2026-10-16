namespace IntentMatcher.Domain.Queries;

public sealed class InvalidQueryException(string message) : Exception(message);
