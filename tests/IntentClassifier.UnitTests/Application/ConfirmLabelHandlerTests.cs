namespace IntentClassifier.UnitTests.Application;

using IntentClassifier.Application.Queries;
using IntentClassifier.Domain.Embeddings;
using IntentClassifier.Domain.Queries;

// The label rules themselves (missing log row, unknown intent, test-split leakage, already
// labelled in the dataset) now live in IClassifiedQueryLog.Confirm so they run under one
// transaction - see ClassifiedQueryLogTests for that DB-level coverage. This handler only
// normalizes input and delegates.
public sealed class ConfirmLabelHandlerTests
{
    [Fact]
    public async Task Normalizes_the_query_and_intent_before_delegating_to_the_log()
    {
        var log = new InMemoryClassifiedQueryLog();
        log.Seed(QueryText.Create("how do i locate my card?"), new Embedding([0.1f, 0.2f]));
        var handler = new ConfirmLabelHandler(log);

        var result = await handler.Handle("  How do I locate my CARD? ", "Card_Arrival");

        Assert.Equal("how do i locate my card?", result.Text.Value);
        Assert.Equal("card_arrival", result.IntentId.Value);
        Assert.Equal(1, log.ConfirmCalls);
    }

    [Fact]
    public async Task Invalid_query_text_is_rejected_before_calling_the_log()
    {
        var log = new InMemoryClassifiedQueryLog();
        var handler = new ConfirmLabelHandler(log);

        await Assert.ThrowsAsync<InvalidQueryException>(() =>
            handler.Handle("   ", "card_arrival")
        );
        Assert.Equal(0, log.ConfirmCalls);
    }

    [Fact]
    public async Task Invalid_intent_id_format_is_rejected_before_calling_the_log()
    {
        var log = new InMemoryClassifiedQueryLog();
        log.Seed(QueryText.Create("some query"), new Embedding([0.1f, 0.2f]));
        var handler = new ConfirmLabelHandler(log);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle("some query", "!!!"));
        Assert.Equal(0, log.ConfirmCalls);
    }

    [Fact]
    public async Task A_rejection_from_the_log_propagates_unchanged()
    {
        var handler = new ConfirmLabelHandler(new InMemoryClassifiedQueryLog());

        await Assert.ThrowsAsync<InvalidQueryException>(() =>
            handler.Handle("never classified", "card_arrival")
        );
    }
}
