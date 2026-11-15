using Application.Common;
using Domain.Catalogs;

namespace Application.Catalogs;

public sealed class CatalogService(ICatalogReader reader)
{
    public async Task<ListCatalogResult> ListAsync(ListCatalogQuery query, CancellationToken cancellationToken)
    {
        if (!CatalogKinds.All.Contains(query.Kind))
        {
            return new ListCatalogResult.UnknownCatalog();
        }

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : CatalogRules.Normalize(query.Search);
        CatalogEntry? after = null;

        if (query.Cursor is not null)
        {
            if (!Cursor.TryDecode<CatalogCursor>(query.Cursor, out var cursor)
                || cursor.Kind != query.Kind
                || cursor.Search != search)
            {
                return new ListCatalogResult.InvalidCursor();
            }

            after = new CatalogEntry(cursor.Id, cursor.Name, cursor.SortOrder);
        }

        var entries = await reader.ListAsync(query.Kind, search, after, query.Limit + 1, cancellationToken);
        var items = entries.Take(query.Limit).ToList();

        var nextCursor = entries.Count > query.Limit
            ? Cursor.Encode(new CatalogCursor(query.Kind, search, items[^1].SortOrder, items[^1].Name, items[^1].Id))
            : null;

        return new ListCatalogResult.Found(new CatalogPage(items, nextCursor));
    }

    private sealed record CatalogCursor(string Kind, string? Search, short SortOrder, string Name, Guid Id);
}
