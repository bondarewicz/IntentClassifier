namespace IntentClassifier.UnitTests.Domain;

using IntentClassifier.Domain.Intents;

public sealed class IntentIdTests
{
    [Theory]
    [InlineData("card_arrival", "card_arrival")]
    [InlineData("Refund_not_showing_up", "refund_not_showing_up")] // spelled like this in Banking77
    [InlineData("reverted_card_payment?", "reverted_card_payment")] // spelled like this in Banking77
    [InlineData(" top_up_failed ", "top_up_failed")]
    public void Create_canonicalizes_labels(string label, string expected) =>
        Assert.Equal(expected, IntentId.Create(label).Value);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("???")]
    [InlineData("_leading")]
    [InlineData("double__underscore")]
    public void Create_rejects_labels_that_are_not_ids(string? label) =>
        Assert.Throws<ArgumentException>(() => IntentId.Create(label));

    [Fact]
    public void Display_name_is_readable() =>
        Assert.Equal("Card arrival", IntentId.Create("card_arrival").ToDisplayName());

    [Fact]
    public void Ids_differing_only_in_spelling_are_equal() =>
        Assert.Equal(
            IntentId.Create("Refund_not_showing_up"),
            IntentId.Create("refund_not_showing_up")
        );
}
