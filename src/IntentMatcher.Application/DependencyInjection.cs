namespace IntentMatcher.Application;

using IntentMatcher.Application.Embeddings;
using IntentMatcher.Application.Evaluation;
using IntentMatcher.Application.Intents;
using IntentMatcher.Application.Matching;
using IntentMatcher.Application.Queries;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services
            .AddTransient<ClassifyQueryHandler>()
            .AddTransient<EmbedDatasetHandler>()
            .AddTransient<TrainIntentsHandler>()
            .AddTransient<RecomputeMatchesHandler>()
            .AddTransient<EvaluateHandler>();
}
