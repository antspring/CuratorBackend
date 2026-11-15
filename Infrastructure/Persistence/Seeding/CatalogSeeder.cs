using Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeding;

internal static class CatalogSeeder
{
    public static void Seed(DbContext context, bool storeManagementPerformed)
    {
        var db = (AppDbContext)context;
        var data = CatalogSeedData.Load();

        Apply(db, data, db.Skills.ToList(), db.Cities.ToList(), db.CatalogItems.ToList());
        db.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, bool storeManagementPerformed, CancellationToken cancellationToken)
    {
        var db = (AppDbContext)context;
        var data = CatalogSeedData.Load();

        Apply(
            db,
            data,
            await db.Skills.ToListAsync(cancellationToken),
            await db.Cities.ToListAsync(cancellationToken),
            await db.CatalogItems.ToListAsync(cancellationToken));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(
        AppDbContext db,
        CatalogSeedData data,
        List<Skill> skills,
        List<City> cities,
        List<CatalogItem> items)
    {
        MergeSearchable(db.Skills, skills, data.Skills, entry => new Skill(entry.Code, entry.Name));
        MergeSearchable(db.Cities, cities, data.Cities, entry => new City(entry.Code, entry.Name));

        var existingItems = items.ToDictionary(item => (item.Kind, item.Code));

        foreach (var (kind, entries) in data.Items)
        {
            for (short sortOrder = 0; sortOrder < entries.Count; sortOrder++)
            {
                var entry = entries[sortOrder];

                if (existingItems.TryGetValue((kind, entry.Code), out var item))
                {
                    if (item.Name != entry.Name || item.SortOrder != sortOrder)
                    {
                        item.Update(entry.Name, sortOrder);
                    }
                }
                else
                {
                    db.CatalogItems.Add(new CatalogItem(kind, entry.Code, entry.Name, sortOrder));
                }
            }
        }
    }

    private static void MergeSearchable<TEntry>(
        DbSet<TEntry> set,
        List<TEntry> existing,
        IReadOnlyList<CatalogSeedEntry> entries,
        Func<CatalogSeedEntry, TEntry> create)
        where TEntry : SearchableCatalogEntry
    {
        var byCode = existing.ToDictionary(entry => entry.Code);

        foreach (var entry in entries)
        {
            if (byCode.TryGetValue(entry.Code, out var current))
            {
                if (current.Name != entry.Name)
                {
                    current.Rename(entry.Name);
                }
            }
            else
            {
                set.Add(create(entry));
            }
        }
    }
}
