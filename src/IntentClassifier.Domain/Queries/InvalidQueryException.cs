namespace IntentClassifier.Domain.Queries;

public sealed class InvalidQueryException(string message) : Exception(message);
