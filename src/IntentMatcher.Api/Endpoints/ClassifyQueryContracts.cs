namespace IntentMatcher.Api.Endpoints;

using IntentMatcher.Application.Queries;

public sealed record ClassifyQueryRequest(string? Query);

public sealed record IntentMatchResponse(
    int Rank,
    string IntentId,
    string IntentName,
    double Similarity
);

public sealed record ClassifyQueryResponse(
    string Query,
    string NormalizedQuery,
    string EmbeddingSource,
    bool Ambiguous,
    IReadOnlyList<IntentMatchResponse> Matches
)
{
    public static ClassifyQueryResponse From(ClassifyQueryResult result) =>
        new(
            result.RawQuery,
            result.Text.Value,
            result.EmbeddingSource == Application.Queries.EmbeddingSource.Stored
                ? "stored"
                : "openai",
            result.IsAmbiguous,
            result
                .Matches.Select(m => new IntentMatchResponse(
                    m.Rank,
                    m.IntentId.Value,
                    m.IntentName,
                    m.Similarity
                ))
                .ToList()
        );
}
