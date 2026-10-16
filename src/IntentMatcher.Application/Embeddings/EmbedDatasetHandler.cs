namespace IntentMatcher.Application.Embeddings;

using IntentMatcher.Application.Abstractions;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Queries;

public sealed record EmbedDatasetProgress(string Stage, int Embedded, int Total);

public sealed record EmbedDatasetResult(int IntentNames, int Queries);

// Embeds intent names and labelled queries that have no embedding yet. Each batch is saved before
// the next one, so an interrupted run resumes where it stopped.
public sealed class EmbedDatasetHandler(
    IIntentRepository intents,
    ILabelledQueryRepository queries,
    IEmbeddingGenerator embeddingGenerator
)
{
    public async Task<EmbedDatasetResult> Handle(
        int batchSize,
        IProgress<EmbedDatasetProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        var pendingIntents = await intents.ListWithoutNameEmbedding(ct);
        var embeddedIntents = 0;
        foreach (var batch in pendingIntents.Chunk(batchSize))
        {
            var embeddings = await embeddingGenerator.GenerateBatch(batch.Select(i => i.Name).ToList(), ct);
            for (var i = 0; i < batch.Length; i++)
                batch[i].AssignNameEmbedding(embeddings[i]);

            await intents.SaveNameEmbeddings(batch, ct);
            embeddedIntents += batch.Length;
            progress?.Report(new EmbedDatasetProgress("intents", embeddedIntents, pendingIntents.Count));
        }

        var pendingQueries = await queries.ListWithoutEmbedding(ct);
        var embeddedQueries = 0;
        foreach (var batch in pendingQueries.Chunk(batchSize))
        {
            var embeddings = await embeddingGenerator.GenerateBatch(batch.Select(q => q.Text.Value).ToList(), ct);
            for (var i = 0; i < batch.Length; i++)
                batch[i].AssignEmbedding(embeddings[i]);

            await queries.SaveEmbeddings(batch, ct);
            embeddedQueries += batch.Length;
            progress?.Report(new EmbedDatasetProgress("queries", embeddedQueries, pendingQueries.Count));
        }

        return new EmbedDatasetResult(embeddedIntents, embeddedQueries);
    }
}
