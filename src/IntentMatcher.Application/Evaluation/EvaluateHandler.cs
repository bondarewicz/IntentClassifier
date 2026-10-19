namespace IntentMatcher.Application.Evaluation;

using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Matching;
using IntentMatcher.Domain.Queries;

// Accuracy of each intent representation on the held-out test split.
public sealed class EvaluateHandler(IEvaluationQueries evaluation)
{
    // How many ranked candidates count toward Top3Correct/Top3Accuracy.
    private const int TopN = 3;

    public async Task<IReadOnlyList<EvaluationResult>> Handle(CancellationToken ct = default)
    {
        var results = new List<EvaluationResult>();
        foreach (var representation in Enum.GetValues<IntentRepresentation>())
            results.Add(
                await evaluation.Evaluate(
                    representation,
                    DatasetSplit.Test,
                    MatchConfidence.AmbiguousMargin,
                    TopN,
                    ct
                )
            );
        return results;
    }
}
