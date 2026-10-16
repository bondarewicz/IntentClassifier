namespace IntentMatcher.Domain.Matching;

public static class MatchConfidence
{
    // When the best intent is less than this ahead of the runner-up the match is ambiguous:
    // the embedding alone cannot tell the two apart reliably.
    public const double AmbiguousMargin = 0.02;

    public static bool IsAmbiguous(IReadOnlyList<IntentMatch> matches) =>
        matches.Count >= 2 && matches[0].Similarity - matches[1].Similarity < AmbiguousMargin;
}
