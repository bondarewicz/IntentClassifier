namespace IntentClassifier.Application.Abstractions;

public interface IDatabaseMigrator
{
    // Applies all pending schema migrations.
    void MigrateUp();
}
