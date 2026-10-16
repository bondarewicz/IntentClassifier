namespace IntentMatcher.Application.Abstractions;

public sealed record DatasetImportResult(long Intents, long TrainQueries, long TestQueries);

// Loads the Banking77 CSVs (text,category). Re-runnable: existing rows are kept with their embeddings.
public interface IDatasetImporter
{
    Task<DatasetImportResult> ImportBanking77(string trainCsvPath, string testCsvPath, CancellationToken ct = default);
}
