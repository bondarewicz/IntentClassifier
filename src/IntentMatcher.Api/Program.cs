using IntentMatcher.Api.Endpoints;
using IntentMatcher.Application;
using IntentMatcher.Application.Abstractions;
using IntentMatcher.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// appsettings.json < user-secrets (shared with the CLI) < environment variables.
builder.Configuration.AddUserSecrets<Program>(optional: true).AddEnvironmentVariables();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddApplication().AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Configuration.GetValue("Database:AutoMigrate", false))
    app.Services.GetRequiredService<IDatabaseMigrator>().MigrateUp();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapClassifyQueryEndpoints();

app.Run();

// Exposed for WebApplicationFactory-based tests.
public partial class Program;
