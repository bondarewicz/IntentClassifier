namespace IntentMatcher.Infrastructure.Embeddings;

using IntentMatcher.Application.Abstractions;
using IntentMatcher.Domain.Embeddings;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

internal sealed class OpenAiEmbeddingGenerator(IOptions<OpenAiOptions> options) : IEmbeddingGenerator
{
    // OpenAI accepts at most 2048 inputs per request.
    private const int MaxInputsPerRequest = 2048;

    private readonly Lazy<EmbeddingClient?> client = new(() =>
        string.IsNullOrEmpty(options.Value.ApiKey) ? null : new EmbeddingClient(options.Value.Model, options.Value.ApiKey)
    );

    public async Task<Embedding> Generate(string text, CancellationToken ct = default)
    {
        var response = await Client.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return new Embedding(response.Value.ToFloats().Span);
    }

    public async Task<IReadOnlyList<Embedding>> GenerateBatch(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var embeddings = new List<Embedding>(texts.Count);
        foreach (var chunk in texts.Chunk(MaxInputsPerRequest))
        {
            var response = await Client.GenerateEmbeddingsAsync(chunk, cancellationToken: ct);
            embeddings.AddRange(response.Value.OrderBy(e => e.Index).Select(e => new Embedding(e.ToFloats().Span)));
        }

        return embeddings;
    }

    private EmbeddingClient Client => client.Value ?? throw new EmbeddingUnavailableException();
}
