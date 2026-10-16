namespace IntentMatcher.UnitTests.Application;

using IntentMatcher.Application.Abstractions;
using IntentMatcher.Application.Queries;
using IntentMatcher.Domain;
using IntentMatcher.Domain.Embeddings;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Queries;

public class ClassifyQueryHandlerTests
{
    private static LabelledQuery Known(string text, Embedding? embedding) =>
        new(Guid.NewGuid(), QueryText.Create(text), DatasetSplit.Test, IntentId.Create("card_arrival"), embedding);

    [Fact]
    public async Task Known_query_reuses_the_stored_embedding_without_generating_one()
    {
        var stored = new Embedding([0.1f, 0.2f]);
        var intents = new FakeIntentRepository();
        var generator = new FakeEmbeddingGenerator();
        var handler = new ClassifyQueryHandler(new InMemoryLabelledQueryRepository(Known("how do i locate my card?", stored)), intents, generator);

        var result = await handler.Handle("  How do I locate my CARD? ", 3);

        Assert.Equal(EmbeddingSource.Stored, result.EmbeddingSource);
        Assert.Equal("how do i locate my card?", result.Text.Value);
        Assert.Same(stored, intents.LastQuery);
        Assert.Empty(generator.Requested);
    }

    [Fact]
    public async Task Unknown_query_is_embedded_from_its_normalized_text()
    {
        var intents = new FakeIntentRepository();
        var generator = new FakeEmbeddingGenerator();
        var handler = new ClassifyQueryHandler(new InMemoryLabelledQueryRepository(), intents, generator);

        var result = await handler.Handle("Charged TWICE for coffee", 3);

        Assert.Equal(EmbeddingSource.Generated, result.EmbeddingSource);
        Assert.Equal(["charged twice for coffee"], generator.Requested);
        Assert.Equal([1, 2, 3], result.Matches.Select(m => m.Rank));
    }

    [Fact]
    public async Task Uses_centroids_unless_asked_otherwise()
    {
        var intents = new FakeIntentRepository();
        var handler = new ClassifyQueryHandler(new InMemoryLabelledQueryRepository(), intents, new FakeEmbeddingGenerator());

        await handler.Handle("hello", 3);
        Assert.Equal(IntentRepresentation.Centroid, intents.LastRepresentation);

        await handler.Handle("hello", 3, IntentRepresentation.Name);
        Assert.Equal(IntentRepresentation.Name, intents.LastRepresentation);
    }

    [Fact]
    public async Task Flags_ambiguity_even_when_only_the_best_match_is_requested()
    {
        var intents = new FakeIntentRepository { Similarities = [0.80, 0.79, 0.5] };
        var handler = new ClassifyQueryHandler(new InMemoryLabelledQueryRepository(), intents, new FakeEmbeddingGenerator());

        var result = await handler.Handle("refund", top: 1);

        Assert.True(result.IsAmbiguous);
        Assert.Single(result.Matches);
        Assert.Equal(2, intents.LastTop);
    }

    [Fact]
    public async Task Invalid_query_is_rejected_before_any_lookup()
    {
        var generator = new FakeEmbeddingGenerator();
        var handler = new ClassifyQueryHandler(new InMemoryLabelledQueryRepository(), new FakeIntentRepository(), generator);

        await Assert.ThrowsAsync<InvalidQueryException>(() => handler.Handle("   ", 3));
        Assert.Empty(generator.Requested);
    }

    [Fact]
    public async Task Unknown_query_without_generator_surfaces_unavailable()
    {
        var handler = new ClassifyQueryHandler(
            new InMemoryLabelledQueryRepository(),
            new FakeIntentRepository(),
            new FakeEmbeddingGenerator(available: false)
        );

        await Assert.ThrowsAsync<EmbeddingUnavailableException>(() => handler.Handle("brand new question", 3));
    }
}
