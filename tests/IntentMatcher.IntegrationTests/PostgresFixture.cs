namespace IntentMatcher.IntegrationTests;

using IntentMatcher.Application;
using IntentMatcher.Application.Abstractions;
using IntentMatcher.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

// One throwaway pgvector Postgres per test class, migrated and seeded with a tiny Banking77-shaped dataset.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("pgvector/pgvector:pg17")
        .WithDatabase("banking77_test")
        .Build();

    public ServiceProvider Services { get; private set; } = null!;

    public string DataDirectory { get; } = Path.Combine(Path.GetTempPath(), $"intentmatcher-{Guid.NewGuid():N}");

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Postgres"] = container.GetConnectionString() })
            .Build();

        Services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(configuration)
            .AddSingleton<IEmbeddingGenerator, HashingEmbeddingGenerator>()   // replaces OpenAI
            .BuildServiceProvider();

        Directory.CreateDirectory(DataDirectory);
        await File.WriteAllTextAsync(TrainCsv, """
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
            """);
        await File.WriteAllTextAsync(TestCsv, """
            text,category
            Has my card arrived yet?,card_arrival
            I see the same payment charged twice,transaction_charged_twice
            The refund is still not showing,Refund_not_showing_up
            "Hmm, my card payment got reverted",reverted_card_payment?
            My card has not arrived yet,card_arrival
            """);

        Services.GetRequiredService<IDatabaseMigrator>().MigrateUp();
    }

    public string TrainCsv => Path.Combine(DataDirectory, "train.csv");

    public string TestCsv => Path.Combine(DataDirectory, "test.csv");

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await container.DisposeAsync();
        Directory.Delete(DataDirectory, recursive: true);
    }
}
