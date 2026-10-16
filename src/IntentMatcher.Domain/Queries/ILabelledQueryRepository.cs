namespace IntentMatcher.Domain.Queries;

public interface ILabelledQueryRepository
{
    // Any stored query with this text (either split); used to reuse its embedding.
    Task<LabelledQuery?> FindByText(QueryText text, CancellationToken ct = default);

    Task<IReadOnlyList<LabelledQuery>> ListWithoutEmbedding(CancellationToken ct = default);

    Task SaveEmbeddings(IReadOnlyCollection<LabelledQuery> queries, CancellationToken ct = default);
}
