using Application.Catalogs;
using Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal sealed class CatalogReader(AppDbContext db) : ICatalogReader, ICatalogLookup
{
    public async Task<IReadOnlyList<CatalogEntry>> ListAsync(
        string kind,
        string? normalizedSearch,
        CatalogEntry? after,
        int take,
        CancellationToken cancellationToken)
    {
        var rows = Rows(kind);

        if (normalizedSearch is not null)
        {
            rows = rows.Where(row => row.NormalizedName.Contains(normalizedSearch));
        }

        if (after is not null)
        {
            rows = rows.Where(row => EF.Functions.GreaterThan(
                ValueTuple.Create(row.SortOrder, row.Name, row.Id),
                ValueTuple.Create(after.SortOrder, after.Name, after.Id)));
        }

        return await rows
            .OrderBy(row => row.SortOrder)
            .ThenBy(row => row.Name)
            .ThenBy(row => row.Id)
            .Take(take)
            .Select(row => new CatalogEntry(row.Id, row.Name, row.SortOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, CatalogRef>> FindAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        var skills = await db.Skills.AsNoTracking()
            .Where(skill => ids.Contains(skill.Id))
            .Select(skill => new CatalogRef(skill.Id, CatalogKinds.Skills, skill.Name, 0))
            .ToListAsync(cancellationToken);

        var cities = await db.Cities.AsNoTracking()
            .Where(city => ids.Contains(city.Id))
            .Select(city => new CatalogRef(city.Id, CatalogKinds.Cities, city.Name, 0))
            .ToListAsync(cancellationToken);

        var items = await db.CatalogItems.AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .Select(item => new CatalogRef(item.Id, item.Kind, item.Name, item.SortOrder))
            .ToListAsync(cancellationToken);

        return skills.Concat(cities).Concat(items).ToDictionary(found => found.Id);
    }

    private IQueryable<Row> Rows(string kind) => kind switch
    {
        CatalogKinds.Skills => db.Skills.AsNoTracking().Select(skill => new Row
        {
            Id = skill.Id,
            Name = skill.Name,
            NormalizedName = skill.NormalizedName,
            SortOrder = 0,
        }),
        CatalogKinds.Cities => db.Cities.AsNoTracking().Select(city => new Row
        {
            Id = city.Id,
            Name = city.Name,
            NormalizedName = city.NormalizedName,
            SortOrder = 0,
        }),
        _ => db.CatalogItems.AsNoTracking().Where(item => item.Kind == kind).Select(item => new Row
        {
            Id = item.Id,
            Name = item.Name,
            NormalizedName = item.Name.ToUpper(),
            SortOrder = item.SortOrder,
        }),
    };

    private sealed class Row
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = null!;

        public string NormalizedName { get; init; } = null!;

        public short SortOrder { get; init; }
    }
}
