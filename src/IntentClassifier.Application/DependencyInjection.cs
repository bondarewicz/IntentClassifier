namespace IntentClassifier.Application;

using IntentClassifier.Application.Embeddings;
using IntentClassifier.Application.Evaluation;
using IntentClassifier.Application.Intents;
using IntentClassifier.Application.Matching;
using IntentClassifier.Application.Queries;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services
            .AddTransient<ClassifyQueryHandler>()
            .AddTransient<ConfirmLabelHandler>()
            .AddTransient<EmbedDatasetHandler>()
            .AddTransient<TrainIntentsHandler>()
            .AddTransient<RecomputeMatchesHandler>()
            .AddTransient<EvaluateHandler>();
}
