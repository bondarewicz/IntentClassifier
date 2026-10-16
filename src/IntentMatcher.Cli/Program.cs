using IntentMatcher.Application;
using IntentMatcher.Application.Abstractions;
using IntentMatcher.Application.Embeddings;
using IntentMatcher.Application.Evaluation;
using IntentMatcher.Application.Intents;
using IntentMatcher.Application.Matching;
using IntentMatcher.Application.Queries;
using IntentMatcher.Domain;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

const string usage = """
    Usage:
      IntentMatcher.Cli migrate
      IntentMatcher.Cli seed [--train data/banking77/train.csv] [--test data/banking77/test.csv]
      IntentMatcher.Cli embed [--batch 1000]
      IntentMatcher.Cli train
      IntentMatcher.Cli match [--top 5] [--by centroid|name]
      IntentMatcher.Cli evaluate
      IntentMatcher.Cli classify "<customer query>" [--top 3] [--by centroid|name]
    """;

if (args.Length == 0)
{
    Console.Error.WriteLine(usage);
    return 1;
}

var command = args[0];
var positional = new List<string>();
int? top = null;
var batchSize = 1000;
var representation = IntentRepresentation.Centroid;
var trainPath = Path.Combine("data", "banking77", "train.csv");
var testPath = Path.Combine("data", "banking77", "test.csv");

for (var i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--top" when i + 1 < args.Length:
            top = int.Parse(args[++i]);
            break;
        case "--batch" when i + 1 < args.Length:
            batchSize = int.Parse(args[++i]);
            break;
        case "--by" when i + 1 < args.Length:
            representation = Enum.Parse<IntentRepresentation>(args[++i], ignoreCase: true);
            break;
        case "--train" when i + 1 < args.Length:
            trainPath = args[++i];
            break;
        case "--test" when i + 1 < args.Length:
            testPath = args[++i];
            break;
        default:
            positional.Add(args[i]);
            break;
    }
}

// appsettings.json (next to the binary) < user-secrets (shared with the API) < environment variables.
var builder = Host.CreateApplicationBuilder(
    new HostApplicationBuilderSettings { Args = [], ContentRootPath = AppContext.BaseDirectory }
);
builder.Configuration.AddUserSecrets<Program>(optional: true).AddEnvironmentVariables();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Services.AddApplication().AddInfrastructure(builder.Configuration);

using var host = builder.Build();
var services = host.Services;

try
{
    return command switch
    {
        "migrate" => Migrate(),
        "seed" => await Seed(),
        "embed" => await Embed(),
        "train" => await Train(),
        "match" => await Match(top ?? 5),
        "evaluate" => await Evaluate(),
        "classify" when positional.Count > 0 => await Classify(
            string.Join(' ', positional),
            top ?? 3
        ),
        _ => Usage(),
    };
}
catch (Exception e) when (e is InvalidQueryException or EmbeddingUnavailableException)
{
    Console.Error.WriteLine(e.Message);
    return 2;
}

int Usage()
{
    Console.Error.WriteLine(usage);
    return 1;
}

int Migrate()
{
    services.GetRequiredService<IDatabaseMigrator>().MigrateUp();
    Console.WriteLine("Database is at the latest migration.");
    return 0;
}

async Task<int> Seed()
{
    var result = await services
        .GetRequiredService<IDatasetImporter>()
        .ImportBanking77(trainPath, testPath);
    Console.WriteLine(
        $"Intents: {result.Intents}, train queries: {result.TrainQueries:N0}, test queries: {result.TestQueries:N0}"
    );
    return 0;
}

async Task<int> Embed()
{
    var result = await services
        .GetRequiredService<EmbedDatasetHandler>()
        .Handle(
            batchSize,
            new ConsoleProgress<EmbedDatasetProgress>(p =>
                Console.WriteLine($"  {p.Stage}: {p.Embedded}/{p.Total}")
            )
        );
    Console.WriteLine(
        $"Embedded {result.IntentNames} intent names and {result.Queries:N0} queries."
    );
    return 0;
}

async Task<int> Train()
{
    var intents = await services.GetRequiredService<TrainIntentsHandler>().Handle();
    Console.WriteLine($"Recomputed centroids for {intents} intents.");
    return 0;
}

async Task<int> Match(int k)
{
    var started = DateTime.UtcNow;
    var result = await services
        .GetRequiredService<RecomputeMatchesHandler>()
        .Handle(k, representation);

    Console.WriteLine(
        $"Stored {result.RowsStored:N0} rows (top {k} per query, by {representation}) in {(DateTime.UtcNow - started).TotalSeconds:F1}s."
    );
    foreach (var s in result.Summary)
        Console.WriteLine(
            $"  {s.Split, -5} queries {s.Queries, 6:N0}  accuracy {(double)s.Correct / s.Queries:P1}  "
                + $"avg best {s.AverageBestSimilarity:F3}  ambiguous {(double)s.Ambiguous / s.Queries:P1}"
        );
    return 0;
}

async Task<int> Evaluate()
{
    var results = await services.GetRequiredService<EvaluateHandler>().Handle();

    Console.WriteLine("Test split (held out):");
    foreach (var r in results)
        Console.WriteLine(
            $"  by {r.Representation, -8} top-1 {r.Top1Accuracy:P1}  top-3 {r.Top3Accuracy:P1}  "
                + $"ambiguous {r.Ambiguous}/{r.Queries} (top-1 {r.AmbiguousTop1Accuracy:P1} vs {r.ConfidentTop1Accuracy:P1} when confident)"
        );
    return 0;
}

async Task<int> Classify(string rawQuery, int k)
{
    var result = await services
        .GetRequiredService<ClassifyQueryHandler>()
        .Handle(rawQuery, k, representation);

    Console.WriteLine($"Query:      {result.RawQuery}");
    Console.WriteLine($"Normalized: {result.Text}");
    Console.WriteLine(
        $"Embedding:  {result.EmbeddingSource}, by {representation}{(result.IsAmbiguous ? ", AMBIGUOUS" : "")}"
    );
    foreach (var m in result.Matches)
        Console.WriteLine($"  {m.Rank, 2}. {m.Similarity:F4}  {m.IntentName} ({m.IntentId})");
    return 0;
}

sealed class ConsoleProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
