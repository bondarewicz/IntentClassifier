namespace IntentMatcher.UnitTests.Domain;

using IntentMatcher.Domain.Embeddings;

public class EmbeddingTests
{
    [Fact]
    public void Copies_values_so_the_source_array_cannot_mutate_it()
    {
        var source = new[] { 1f, 2f, 3f };
        var embedding = new Embedding(source);

        source[0] = 99f;

        Assert.Equal(new[] { 1f, 2f, 3f }, embedding.Values.ToArray());
        Assert.Equal(3, embedding.Dimensions);
    }

    [Fact]
    public void Rejects_empty_vectors() => Assert.Throws<ArgumentException>(() => new Embedding([]));
}
