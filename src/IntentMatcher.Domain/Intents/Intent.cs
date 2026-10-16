namespace IntentMatcher.Domain.Intents;

using IntentMatcher.Domain.Embeddings;

public sealed class Intent(IntentId id, string name, Embedding? nameEmbedding = null)
{
    public IntentId Id { get; } = id;

    public string Name { get; } = name;

    // Embedding of the readable name (the scan-group approach: label text only).
    public Embedding? NameEmbedding { get; private set; } = nameEmbedding;

    public bool HasNameEmbedding => NameEmbedding is not null;

    public void AssignNameEmbedding(Embedding embedding) =>
        NameEmbedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
}
