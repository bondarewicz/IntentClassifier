namespace IntentMatcher.Domain.Matching;

using IntentMatcher.Domain.Intents;

// Stored top-K intent matches for every query in the dataset.
public interface IIntentMatchRepository
{
    // Replaces all stored matches with each embedded query's top-K intents; returns rows stored.
    Task<long> RecomputeAll(
        int top,
        IntentRepresentation representation,
        CancellationToken ct = default
    );
}
