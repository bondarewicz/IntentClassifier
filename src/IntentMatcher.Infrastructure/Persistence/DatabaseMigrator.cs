namespace IntentMatcher.Infrastructure.Persistence;

using FluentMigrator.Runner;
using IntentMatcher.Application.Abstractions;
using IntentMatcher.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

internal sealed class DatabaseMigrator(IServiceScopeFactory scopes) : IDatabaseMigrator
{
    public void MigrateUp()
    {
        using var scope = scopes.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
    }

    // The connection string is resolved per scope from IConfiguration (not captured at
    // registration time), so a test host's configuration override still takes effect.
    public static void AddTo(IServiceCollection services) =>
        services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb =>
                rb.AddPostgres()
                    .WithGlobalConnectionString(sp =>
                        DependencyInjection.ReadPostgresConnectionString(
                            sp.GetRequiredService<IConfiguration>()
                        )
                    )
                    .ScanIn(typeof(EnableVectorExtension).Assembly)
                    .For.Migrations()
            );
}
