namespace IntentMatcher.Infrastructure.Persistence;

using FluentMigrator.Runner;
using IntentMatcher.Application.Abstractions;
using IntentMatcher.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.DependencyInjection;

internal sealed class DatabaseMigrator(IServiceScopeFactory scopes) : IDatabaseMigrator
{
    public void MigrateUp()
    {
        using var scope = scopes.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
    }

    public static void AddTo(IServiceCollection services, string connectionString) =>
        services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(M001_EnableVectorExtension).Assembly).For.Migrations());
}
