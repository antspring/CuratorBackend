namespace Application.Catalogs;

public interface ICatalogLookup
{
    Task<IReadOnlyDictionary<Guid, CatalogRef>> FindAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}

public sealed record CatalogRef(Guid Id, string Kind, string Name, short SortOrder);
