namespace IntentMatcher.Domain.Queries;

// Train queries define the intent centroids; test queries are held out to measure accuracy.
public enum DatasetSplit
{
    Train,
    Test
}
