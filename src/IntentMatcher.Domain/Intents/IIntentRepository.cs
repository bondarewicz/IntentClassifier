namespace IntentMatcher.Domain.Intents;

using IntentMatcher.Domain.Embeddings;
using IntentMatcher.Domain.Matching;

public interface IIntentRepository
{
    Task<IReadOnlyList<Intent>> ListWithoutNameEmbedding(CancellationToken ct = default);

    Task SaveNameEmbeddings(IReadOnlyCollection<Intent> intents, CancellationToken ct = default);

    // Recomputes every intent's centroid from its embedded train queries; returns intents updated.
    Task<int> RecomputeCentroids(CancellationToken ct = default);

    // Intents ordered by cosine similarity to the embedding, closest first.
    Task<IReadOnlyList<IntentMatch>> FindClosest(
        Embedding embedding,
        int top,
        IntentRepresentation representation,
        CancellationToken ct = default
    );
}
