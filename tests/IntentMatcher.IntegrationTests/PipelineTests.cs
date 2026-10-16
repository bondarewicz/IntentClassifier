namespace IntentMatcher.IntegrationTests;

using IntentMatcher.Application.Abstractions;
using IntentMatcher.Application.Embeddings;
using IntentMatcher.Application.Evaluation;
using IntentMatcher.Application.Intents;
using IntentMatcher.Application.Matching;
using IntentMatcher.Application.Queries;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Queries;
using Microsoft.Extensions.DependencyInjection;

// Runs the real migrations, COPY import, Dapper repositories and pgvector SQL end to end.
// Ordered as one scenario because each step builds on the previous one.
public sealed class PipelineTests(PostgresFixture db) : IClassFixture<PostgresFixture>
{
    private T Get<T>()
        where T : notnull => db.Services.GetRequiredService<T>();

    [Fact]
    public async Task Full_pipeline_from_empty_database_to_classification()
    {
        // migrations are idempotent
        Get<IDatabaseMigrator>().MigrateUp();

        // seed: labels canonicalized, duplicate train text collapsed, the train/test overlap kept in both
        var seeded = await Get<IDatasetImporter>().ImportBanking77(db.TrainCsv, db.TestCsv);
        Assert.Equal(new DatasetImportResult(Intents: 4, TrainQueries: 10, TestQueries: 5), seeded);
        Assert.Equal(
            seeded,
            await Get<IDatasetImporter>().ImportBanking77(db.TrainCsv, db.TestCsv)
        ); // re-runnable

        // embed: names + queries, then nothing left
        var embedded = await Get<EmbedDatasetHandler>().Handle(batchSize: 4);
        Assert.Equal(new EmbedDatasetResult(IntentNames: 4, Queries: 15), embedded);
        Assert.Equal(
            new EmbedDatasetResult(0, 0),
            await Get<EmbedDatasetHandler>().Handle(batchSize: 4)
        );

        // train: every intent gets a centroid
        Assert.Equal(4, await Get<TrainIntentsHandler>().Handle());

        // classify: known text reuses its stored embedding, new text is generated
        var known = await Get<ClassifyQueryHandler>().Handle("My card has NOT arrived yet", 3);
        Assert.Equal(EmbeddingSource.Stored, known.EmbeddingSource);
        Assert.Equal("card_arrival", known.Matches[0].IntentId.Value);
        Assert.Equal([1, 2, 3], known.Matches.Select(m => m.Rank));
        Assert.True(known.Matches[0].Similarity >= known.Matches[1].Similarity);

        var fresh = await Get<ClassifyQueryHandler>().Handle("why is my refund not showing", 3);
        Assert.Equal(EmbeddingSource.Generated, fresh.EmbeddingSource);
        Assert.Equal("refund_not_showing_up", fresh.Matches[0].IntentId.Value);

        // match: top-K stored for every query, summary per split read back through the report view
        var matched = await Get<RecomputeMatchesHandler>().Handle(top: 3);
        Assert.Equal(15 * 3, matched.RowsStored);
        Assert.Equal([DatasetSplit.Train, DatasetSplit.Test], matched.Summary.Select(s => s.Split));
        Assert.Equal(10, matched.Summary[0].Queries);
        Assert.Equal(5, matched.Summary[1].Queries);

        // evaluate: both representations over the test split
        var evaluation = await Get<EvaluateHandler>().Handle();
        Assert.Equal(
            [IntentRepresentation.Centroid, IntentRepresentation.Name],
            evaluation.Select(e => e.Representation)
        );
        Assert.All(evaluation, e => Assert.Equal(5, e.Queries));
        var centroid = evaluation[0];
        Assert.True(centroid.Top1Correct >= 4, $"centroid top-1 was {centroid.Top1Correct}/5");
        Assert.True(centroid.Top3Correct >= centroid.Top1Correct);
    }
}
