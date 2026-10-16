namespace IntentMatcher.IntegrationTests;

using IntentMatcher.Application.Abstractions;
using IntentMatcher.Domain.Embeddings;

// Deterministic stand-in for OpenAI: a bag-of-words hashed into 1536 dimensions, so texts sharing
// words are close in cosine space. Lets the real SQL/pgvector pipeline run without network calls.
internal sealed class HashingEmbeddingGenerator : IEmbeddingGenerator
{
    public const int Dimensions = 1536;

    public Task<Embedding> Generate(string text, CancellationToken ct = default) => Task.FromResult(Embed(text));

    public Task<IReadOnlyList<Embedding>> GenerateBatch(IReadOnlyList<string> texts, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Embedding>>(texts.Select(Embed).ToList());

    private static Embedding Embed(string text)
    {
        var vector = new float[Dimensions];
        var words = text.ToLowerInvariant().Split([' ', '?', '.', ',', '!', '\''], StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
            vector[(int)((uint)StableHash(word) % Dimensions)] += 1f;
        vector[Dimensions - 1] += 0.01f;   // never all zeros
        return new Embedding(vector);
    }

    private static int StableHash(string s)
    {
        unchecked
        {
            var hash = (int)2166136261;
            foreach (var c in s)
                hash = (hash ^ c) * 16777619;
            return hash;
        }
    }
}
