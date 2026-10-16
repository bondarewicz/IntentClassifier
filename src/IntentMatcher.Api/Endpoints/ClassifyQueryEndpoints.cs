namespace IntentMatcher.Api.Endpoints;

using IntentMatcher.Application.Abstractions;
using IntentMatcher.Application.Queries;
using IntentMatcher.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

public static class ClassifyQueryEndpoints
{
    private const int TopMatches = 3;

    public static IEndpointRouteBuilder MapClassifyQueryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/queries/classify", Classify)
            .WithName("ClassifyQuery")
            .WithSummary(
                "Top 3 Banking77 intents for a raw customer query, by cosine similarity to the intent centroids."
            );

        return app;
    }

    private static async Task<
        Results<Ok<ClassifyQueryResponse>, ValidationProblem, ProblemHttpResult>
    > Classify(ClassifyQueryRequest request, ClassifyQueryHandler handler, CancellationToken ct)
    {
        try
        {
            var result = await handler.Handle(request.Query ?? string.Empty, TopMatches, ct: ct);
            return TypedResults.Ok(ClassifyQueryResponse.From(result));
        }
        catch (InvalidQueryException e)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["query"] = [e.Message] }
            );
        }
        catch (EmbeddingUnavailableException)
        {
            return TypedResults.Problem(
                "Query is not in the dataset and no OpenAI API key is configured to embed it.",
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }
    }
}
