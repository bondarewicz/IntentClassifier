using IntentMatcher.Application;
using IntentMatcher.Application.Abstractions;
using IntentMatcher.Cli;
using IntentMatcher.Domain.Queries;
using IntentMatcher.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using FluentMigratorException = FluentMigrator.Exceptions.FluentMigratorException;

if (!CliOptions.TryParse(args, out var options, out var error))
{
    Console.Error.WriteLine(error);
    return 1;
}

var builder = Host.CreateApplicationBuilder(
    new HostApplicationBuilderSettings { Args = [], ContentRootPath = AppContext.BaseDirectory }
);

// appsettings.json < OPENAI_API_KEY fallback < user-secrets (shared with the API) < environment
// variables - OPENAI_API_KEY is the lowest-priority source, so OpenAi:ApiKey/OpenAi__ApiKey
// always wins when set.
builder
    .Configuration.AddInMemoryCollection(
        new Dictionary<string, string?>
        {
            ["OpenAi:ApiKey"] = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
        }
    )
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// The generic host already logs a "Hosting failed to start" stack trace for a StartAsync
// failure (e.g. OptionsValidationException) before rethrowing it to our own catch block below,
// which reports the same failure as a clean one-line message - this would otherwise double it.
builder.Logging.AddFilter("Microsoft.Extensions.Hosting.Internal.Host", LogLevel.None);
builder.Services.AddApplication().AddInfrastructure(builder.Configuration);

using var host = builder.Build();

try
{
    // Runs every IHostedService, including the one ValidateOnStart registers - without this,
    // OpenAiOptions is never validated because nothing else starts the host for a console app.
    await host.StartAsync();

    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;

    var exitCode = options!.Command switch
    {
        "migrate" => Commands.Migrate(services),
        "seed" => await Commands.Seed(services, options),
        "embed" => await Commands.Embed(services, options),
        "train" => await Commands.Train(services),
        "match" => await Commands.Match(services, options),
        "evaluate" => await Commands.Evaluate(services),
        "classify" when options.Positional.Count > 0 => await Commands.Classify(services, options),
        "label" when options.Positional.Count == 2 => await Commands.Label(services, options),
        _ => Usage(),
    };

    await host.StopAsync();
    return exitCode;
}
catch (Exception e)
    when (e
            is InvalidQueryException
                or EmbeddingUnavailableException
                or MissingConfigurationException
                or OptionsValidationException
                or NpgsqlException
                or FluentMigratorException
                or ArgumentException
                or FormatException
                or IOException
    )
{
    Console.Error.WriteLine(e.Message);
    return 2;
}

int Usage()
{
    Console.Error.WriteLine(CliOptions.Usage);
    return 1;
}
