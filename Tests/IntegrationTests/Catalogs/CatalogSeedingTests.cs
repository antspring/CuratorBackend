using Domain.Catalogs;
using Infrastructure.Persistence;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Catalogs;

public class CatalogSeedingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Migrate_SeedsAllCatalogs()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await db.Skills.AnyAsync(skill => skill.Code == "csharp" && skill.NormalizedName == "C#"));
        Assert.True(await db.Cities.AnyAsync(city => city.Code == "yekaterinburg" && city.Name == "Екатеринбург"));

        var kinds = await db.CatalogItems.Select(item => item.Kind).Distinct().ToListAsync();
        Assert.Equal(CatalogKinds.All.Order(), kinds.Order());

        var levels = await db.CatalogItems
            .Where(item => item.Kind == CatalogKinds.SkillLevels)
            .OrderBy(item => item.SortOrder)
            .Select(item => item.Code)
            .ToListAsync();
        Assert.Equal(["basic", "confident", "advanced", "expert"], levels);
    }

    [Fact]
    public async Task RepeatedSeeding_DoesNotDuplicateOrReplaceRows()
    {
        var before = await SnapshotAsync();

        await MigrateAgainAsync();

        Assert.Equal(before, await SnapshotAsync());
    }

    [Fact]
    public async Task RepeatedSeeding_RestoresNameAndSortOrderByCode()
    {
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.CatalogItems
                .Where(item => item.Kind == CatalogKinds.Formats && item.Code == "online")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Name, "Удалённо")
                    .SetProperty(item => item.SortOrder, (short)9));
        }

        await MigrateAgainAsync();

        await using var check = factory.Services.CreateAsyncScope();
        var item = await check.ServiceProvider.GetRequiredService<AppDbContext>().CatalogItems
            .SingleAsync(item => item.Kind == CatalogKinds.Formats && item.Code == "online");
        Assert.Equal("Онлайн", item.Name);
        Assert.Equal(0, item.SortOrder);
    }

    [Fact]
    public async Task RepeatedSeeding_KeepsValuesMissingFromSeedData()
    {
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Skills.Add(new Skill("seed-test-only", "Навык только для теста"));
            await db.SaveChangesAsync();
        }

        await MigrateAgainAsync();

        await using var check = factory.Services.CreateAsyncScope();
        Assert.True(await check.ServiceProvider.GetRequiredService<AppDbContext>().Skills
            .AnyAsync(skill => skill.Code == "seed-test-only"));
    }

    private async Task MigrateAgainAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    private async Task<List<string>> SnapshotAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var skills = await db.Skills.Select(skill => $"skill:{skill.Id}:{skill.Code}:{skill.Name}").ToListAsync();
        var cities = await db.Cities.Select(city => $"city:{city.Id}:{city.Code}:{city.Name}").ToListAsync();
        var items = await db.CatalogItems
            .Select(item => $"item:{item.Id}:{item.Kind}:{item.Code}:{item.Name}:{item.SortOrder}")
            .ToListAsync();

        return [.. skills.Concat(cities).Concat(items).Order()];
    }
}
