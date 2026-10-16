namespace IntentMatcher.Api;

using IntentMatcher.Application.Abstractions;
using IntentMatcher.Domain.Queries;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

// Maps the two exceptions a query can fail with to the response they document. Anything else
// falls through to the default ProblemDetails response ASP.NET Core writes for an unhandled 500.
internal sealed class ClassifyQueryExceptionHandler(IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct
    )
    {
        ProblemDetails? problem = exception switch
        {
            InvalidQueryException e => new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["query"] = [e.Message] }
            )
            {
                Status = StatusCodes.Status400BadRequest,
            },
            EmbeddingUnavailableException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail =
                    "Query is not in the dataset and no OpenAI API key is configured to embed it.",
            },
            _ => null,
        };

        if (problem is null)
            return false;

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(
            new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problem }
        );
    }
}
