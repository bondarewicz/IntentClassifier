namespace IntentClassifier.Application.Matching;

using IntentClassifier.Domain.Intents;

// Rebuilds the stored top-K intent matches for every query in the dataset.
public interface IIntentMatchProjection
{
    Task<long> RebuildAll(
        int top,
        IntentRepresentation representation,
        CancellationToken ct = default
    );
}
