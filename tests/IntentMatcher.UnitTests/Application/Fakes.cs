namespace IntentMatcher.UnitTests.Application;

using IntentMatcher.Application.Abstractions;
using IntentMatcher.Domain.Embeddings;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Matching;
using IntentMatcher.Domain.Queries;

internal sealed class InMemoryLabelledQueryRepository(params LabelledQuery[] queries)
    : ILabelledQueryRepository
{
    public List<LabelledQuery> Queries { get; } = [.. queries];
    public List<int> SavedBatchSizes { get; } = [];

    public Task<LabelledQuery?> FindByText(QueryText text, CancellationToken ct = default) =>
        Task.FromResult(Queries.FirstOrDefault(q => q.Text == text));

    public Task<IReadOnlyList<LabelledQuery>> ListWithoutEmbedding(
        CancellationToken ct = default
    ) =>
        Task.FromResult<IReadOnlyList<LabelledQuery>>(Queries.Where(q => !q.HasEmbedding).ToList());

    public Task SaveEmbeddings(
        IReadOnlyCollection<LabelledQuery> queries,
        CancellationToken ct = default
    )
    {
        SavedBatchSizes.Add(queries.Count);
        return Task.CompletedTask;
    }
}

internal sealed class FakeIntentRepository(params Intent[] intents) : IIntentRepository
{
    public List<Intent> Intents { get; } = [.. intents];
    public Embedding? LastQuery { get; private set; }
    public int LastTop { get; private set; }
    public IntentRepresentation? LastRepresentation { get; private set; }

    // Similarities returned by FindClosest, best first.
    public double[] Similarities { get; set; } = [0.9, 0.7, 0.6, 0.5, 0.4];

    public Task<IReadOnlyList<Intent>> ListWithoutNameEmbedding(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Intent>>(Intents.Where(i => !i.HasNameEmbedding).ToList());

    public Task SaveNameEmbeddings(
        IReadOnlyCollection<Intent> intents,
        CancellationToken ct = default
    ) => Task.CompletedTask;

    public Task<int> RecomputeCentroids(CancellationToken ct = default) =>
        Task.FromResult(Intents.Count);

    public Task<IReadOnlyList<IntentMatch>> FindClosest(
        Embedding embedding,
        int top,
        IntentRepresentation representation,
        CancellationToken ct = default
    )
    {
        LastQuery = embedding;
        LastTop = top;
        LastRepresentation = representation;
        IReadOnlyList<IntentMatch> matches = Similarities
            .Take(top)
            .Select(
                (s, i) =>
                    new IntentMatch(i + 1, IntentId.Create($"intent_{i + 1}"), $"Intent {i + 1}", s)
            )
            .ToList();
        return Task.FromResult(matches);
    }
}

internal sealed class FakeEmbeddingGenerator(bool available = true) : IEmbeddingGenerator
{
    public List<string> Requested { get; } = [];

    public Task<Embedding> Generate(string text, CancellationToken ct = default)
    {
        if (!available)
            throw new EmbeddingUnavailableException();
        Requested.Add(text);
        return Task.FromResult(For(text));
    }

    public Task<IReadOnlyList<Embedding>> GenerateBatch(
        IReadOnlyList<string> texts,
        CancellationToken ct = default
    )
    {
        if (!available)
            throw new EmbeddingUnavailableException();
        Requested.AddRange(texts);
        return Task.FromResult<IReadOnlyList<Embedding>>(texts.Select(For).ToList());
    }

    // Deterministic per text, so tests can check which embedding went where.
    public static Embedding For(string text) => new([text.Length, 1f]);
}
