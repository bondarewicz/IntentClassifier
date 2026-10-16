namespace IntentMatcher.UnitTests.Domain;

using IntentMatcher.Domain.Queries;

public sealed class QueryTextTests
{
    [Theory]
    [InlineData("How do I locate my card?", "how do i locate my card?")]
    [InlineData(
        "  I was CHARGED twice\tfor the\n same coffee  ",
        "i was charged twice for the same coffee"
    )]
    [InlineData("Where's my £ refund?!", "where's my £ refund?!")]
    public void Create_trims_collapses_whitespace_lowercases_and_keeps_punctuation(
        string raw,
        string expected
    ) => Assert.Equal(expected, QueryText.Create(raw).Value);

    [Fact]
    public void Create_is_idempotent()
    {
        var once = QueryText.Create("  Why is my TOP-UP pending? ");
        Assert.Equal(once, QueryText.Create(once.Value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\n ")]
    public void Create_rejects_empty_queries(string? raw) =>
        Assert.Throws<InvalidQueryException>(() => QueryText.Create(raw));

    [Fact]
    public void Create_rejects_queries_over_the_maximum_length() =>
        Assert.Throws<InvalidQueryException>(() =>
            QueryText.Create(new string('a', QueryText.MaxLength + 1))
        );
}
