namespace IntentMatcher.UnitTests.Domain;

using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Matching;

public sealed class MatchConfidenceTests
{
    private static IntentMatch Match(int rank, double similarity) =>
        new(rank, IntentId.Create($"intent_{rank}"), $"Intent {rank}", similarity);

    [Fact]
    public void Ambiguous_when_runner_up_is_within_the_margin() =>
        Assert.True(MatchConfidence.IsAmbiguous([Match(1, 0.80), Match(2, 0.79)]));

    [Fact]
    public void Confident_when_best_is_clearly_ahead() =>
        Assert.False(MatchConfidence.IsAmbiguous([Match(1, 0.80), Match(2, 0.70)]));

    [Fact]
    public void A_single_candidate_is_not_ambiguous() =>
        Assert.False(MatchConfidence.IsAmbiguous([Match(1, 0.5)]));
}
