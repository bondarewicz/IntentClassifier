namespace IntentMatcher.Application.Queries;

using IntentMatcher.Application.Abstractions;
using IntentMatcher.Domain.Embeddings;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Matching;
using IntentMatcher.Domain.Queries;

public enum EmbeddingSource
{
    Stored,
    Generated,
}

public sealed record ClassifyQueryResult(
    string RawQuery,
    QueryText Text,
    EmbeddingSource EmbeddingSource,
    bool IsAmbiguous,
    IReadOnlyList<IntentMatch> Matches
);

// Classifies one raw customer query: normalize -> reuse the stored embedding when the text is
// known, otherwise generate one -> closest intents by cosine similarity.
public sealed class ClassifyQueryHandler(
    ILabelledQueryRepository queries,
    IIntentRepository intents,
    IEmbeddingGenerator embeddingGenerator
)
{
    public async Task<ClassifyQueryResult> Handle(
        string rawQuery,
        int top,
        IntentRepresentation representation = IntentRepresentation.Centroid,
        CancellationToken ct = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(top, 1);

        var text = QueryText.Create(rawQuery);
        var known = await queries.FindByText(text, ct);

        Embedding embedding;
        EmbeddingSource source;
        if (known?.Embedding is { } stored)
        {
            embedding = stored;
            source = EmbeddingSource.Stored;
        }
        else
        {
            embedding = await embeddingGenerator.Generate(text.Value, ct);
            source = EmbeddingSource.Generated;
        }

        // Ambiguity needs the runner-up even when only the best match is requested.
        var matches = await intents.FindClosest(embedding, Math.Max(top, 2), representation, ct);
        return new ClassifyQueryResult(
            rawQuery,
            text,
            source,
            MatchConfidence.IsAmbiguous(matches),
            matches.Take(top).ToList()
        );
    }
}
