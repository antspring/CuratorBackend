namespace Application.Catalogs;

public interface ICatalogLookup
{
    Task<IReadOnlyDictionary<Guid, string>> FindKindsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
