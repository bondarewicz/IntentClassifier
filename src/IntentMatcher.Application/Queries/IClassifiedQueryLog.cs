namespace IntentMatcher.Application.Queries;

using IntentMatcher.Domain.Embeddings;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Queries;

public sealed record ClassifiedQuery(Embedding Embedding, IntentId? ConfirmedIntentId);

// Cache and audit trail of every successful classify call, keyed by normalized text. The
// embedding is written once on first classify and never overwritten; Record upserts the rest
// (times_classified, best match, ambiguous) on every repeat.
public interface IClassifiedQueryLog
{
    Task<ClassifiedQuery?> FindByText(QueryText text, CancellationToken ct = default);

    Task Record(
        QueryText text,
        Embedding embedding,
        IntentRepresentation representation,
        IntentId? bestIntentId,
        double? bestSimilarity,
        bool ambiguous,
        CancellationToken ct = default
    );

    // Validates every label rule and promotes the logged query into labelled_queries(train),
    // marking it confirmed - all atomically, under a lock on its log row. Throws
    // InvalidQueryException when: the text has no log row; the intent does not exist; the text
    // is in the test split; or the text is already labelled in the dataset (an unpromoted train
    // row).
    Task Confirm(QueryText text, IntentId intentId, CancellationToken ct = default);
}
