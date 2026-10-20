namespace IntentClassifier.Infrastructure.Embeddings;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "text-embedding-3-small";
}
