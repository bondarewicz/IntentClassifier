namespace IntentMatcher.UnitTests.Application;

using IntentMatcher.Application.Embeddings;
using IntentMatcher.Domain.Embeddings;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Queries;

public class EmbedDatasetHandlerTests
{
    [Fact]
    public async Task Embeds_intent_names_and_only_queries_without_embedding_in_batches()
    {
        var intents = new FakeIntentRepository(
            new Intent(IntentId.Create("card_arrival"), "Card arrival"),
            new Intent(IntentId.Create("top_up_failed"), "Top up failed", new Embedding([1f]))
        );
        var alreadyEmbedded = new LabelledQuery(
            Guid.NewGuid(),
            QueryText.Create("done"),
            DatasetSplit.Train,
            IntentId.Create("card_arrival"),
            new Embedding([1f])
        );
        var pending = Enumerable
            .Range(1, 5)
            .Select(i => new LabelledQuery(
                Guid.NewGuid(),
                QueryText.Create($"query {i}"),
                DatasetSplit.Train,
                IntentId.Create("card_arrival")
            ))
            .ToArray();
        var queries = new InMemoryLabelledQueryRepository([alreadyEmbedded, .. pending]);
        var generator = new FakeEmbeddingGenerator();

        var result = await new EmbedDatasetHandler(intents, queries, generator).Handle(
            batchSize: 2
        );

        Assert.Equal(1, result.IntentNames);
        Assert.Equal(5, result.Queries);
        Assert.Equal([2, 2, 1], queries.SavedBatchSizes);
        Assert.Contains("Card arrival", generator.Requested);
        Assert.DoesNotContain("Top up failed", generator.Requested);
        Assert.DoesNotContain("done", generator.Requested);
        Assert.All(
            pending,
            q =>
                Assert.Equal(
                    FakeEmbeddingGenerator.For(q.Text.Value).Values.ToArray(),
                    q.Embedding!.Values.ToArray()
                )
        );
    }
}
