namespace IntentMatcher.Application.Intents;

using IntentMatcher.Domain.Intents;

// "Training" here is just recomputing each intent's centroid from its embedded train queries.
public sealed class TrainIntentsHandler(IIntentRepository intents)
{
    public Task<int> Handle(CancellationToken ct = default) => intents.RecomputeCentroids(ct);
}
