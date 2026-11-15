namespace Application.Catalogs;

public sealed record CatalogEntry(Guid Id, string Name, short SortOrder);

public sealed record CatalogPage(IReadOnlyList<CatalogEntry> Items, string? NextCursor);

public sealed record ListCatalogQuery(string Kind, string? Search, int Limit, string? Cursor);

public abstract record ListCatalogResult
{
    public sealed record Found(CatalogPage Page) : ListCatalogResult;

    public sealed record UnknownCatalog : ListCatalogResult;

    public sealed record InvalidCursor : ListCatalogResult;
}
