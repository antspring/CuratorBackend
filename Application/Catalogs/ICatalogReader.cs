namespace Application.Catalogs;

public interface ICatalogReader
{
    Task<IReadOnlyList<CatalogEntry>> ListAsync(
        string kind,
        string? normalizedSearch,
        CatalogEntry? after,
        int take,
        CancellationToken cancellationToken);
}
