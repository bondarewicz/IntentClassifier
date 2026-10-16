namespace IntentMatcher.Application.Evaluation;

using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Matching;
using IntentMatcher.Domain.Queries;

// Accuracy of each intent representation on the held-out test split.
public sealed class EvaluateHandler(IEvaluationQueries evaluation)
{
    public async Task<IReadOnlyList<EvaluationResult>> Handle(
        DatasetSplit split = DatasetSplit.Test,
        CancellationToken ct = default
    )
    {
        var results = new List<EvaluationResult>();
        foreach (var representation in Enum.GetValues<IntentRepresentation>())
            results.Add(
                await evaluation.Evaluate(
                    representation,
                    split,
                    MatchConfidence.AmbiguousMargin,
                    ct
                )
            );
        return results;
    }
}
