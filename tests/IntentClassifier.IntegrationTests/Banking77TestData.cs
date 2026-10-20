namespace IntentClassifier.IntegrationTests;

// The tiny Banking77-shaped dataset PostgresFixture seeds: 4 intents, 10 train queries (one
// deliberate duplicate), 5 test queries (one overlapping a train query, to exercise the
// (split, query_text) key), and the two inconsistently spelled labels from the real dataset.
internal static class Banking77TestData
{
    public const string Train = """
        text,category
        I am still waiting on my card?,card_arrival
        When will my new card arrive?,card_arrival
        My card has not arrived yet,card_arrival
        I was charged twice for one payment,transaction_charged_twice
        Why was this transaction charged twice?,transaction_charged_twice
        A payment shows up twice on my statement,transaction_charged_twice
        My refund is not showing up,Refund_not_showing_up
        Where is the refund for my return?,Refund_not_showing_up
        My card payment was reverted,reverted_card_payment?
        Why did the card payment get reverted?,reverted_card_payment?
        My card has not arrived yet,card_arrival
        """;

    public const string Test = """
        text,category
        Has my card arrived yet?,card_arrival
        I see the same payment charged twice,transaction_charged_twice
        The refund is still not showing,Refund_not_showing_up
        "Hmm, my card payment got reverted",reverted_card_payment?
        My card has not arrived yet,card_arrival
        """;
}
