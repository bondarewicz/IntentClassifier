namespace IntentMatcher.Infrastructure;

using IntentMatcher.Application.Abstractions;
using IntentMatcher.Application.Evaluation;
using IntentMatcher.Application.Matching;
using IntentMatcher.Domain.Intents;
using IntentMatcher.Domain.Matching;
using IntentMatcher.Domain.Queries;
using IntentMatcher.Infrastructure.Embeddings;
using IntentMatcher.Infrastructure.Persistence;
using IntentMatcher.Infrastructure.Persistence.Import;
using IntentMatcher.Infrastructure.Persistence.IntentMatches;
using IntentMatcher.Infrastructure.Persistence.Intents;
using IntentMatcher.Infrastructure.Persistence.LabelledQueries;
using IntentMatcher.Infrastructure.Persistence.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        PgvectorDapper.EnsureRegistered();
        services.AddSingleton(_ =>
        {
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.UseVector();
            return builder.Build();
        });

        services.AddSingleton<IIntentRepository, IntentRepository>();
        services.AddSingleton<ILabelledQueryRepository, LabelledQueryRepository>();
        services.AddSingleton<IIntentMatchRepository, IntentMatchRepository>();
        services.AddSingleton<MatchReportQueries>();
        services.AddSingleton<IMatchReportQueries>(sp =>
            sp.GetRequiredService<MatchReportQueries>()
        );
        services.AddSingleton<IEvaluationQueries>(sp =>
            sp.GetRequiredService<MatchReportQueries>()
        );
        services.AddSingleton<IDatasetImporter, Banking77Importer>();

        DatabaseMigrator.AddTo(services, connectionString);
        services.AddSingleton<IDatabaseMigrator, DatabaseMigrator>();

        services
            .AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .PostConfigure(o =>
            {
                if (string.IsNullOrEmpty(o.ApiKey))
                    o.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            });
        services.AddSingleton<IEmbeddingGenerator, OpenAiEmbeddingGenerator>();

        return services;
    }
}
