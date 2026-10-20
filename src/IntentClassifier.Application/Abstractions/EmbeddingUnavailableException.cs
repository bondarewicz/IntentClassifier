namespace IntentClassifier.Application.Abstractions;

public sealed class EmbeddingUnavailableException()
    : Exception("No embedding generator is configured (OpenAi:ApiKey is not set).");
