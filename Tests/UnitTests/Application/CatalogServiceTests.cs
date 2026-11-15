using Application.Catalogs;
using Domain.Catalogs;

namespace UnitTests.Application;

public class CatalogServiceTests
{
    private readonly FakeCatalogReader reader = new();
    private readonly CatalogService service;

    public CatalogServiceTests()
    {
        service = new CatalogService(reader);
    }

    [Fact]
    public async Task List_UnknownKind_ReturnsUnknownCatalog()
    {
        var result = await service.ListAsync(new ListCatalogQuery("interests", null, 20, null), CancellationToken.None);

        Assert.IsType<ListCatalogResult.UnknownCatalog>(result);
        Assert.Null(reader.LastKind);
    }

    [Fact]
    public async Task List_NormalizesSearchAndRequestsOneExtraEntry()
    {
        await service.ListAsync(new ListCatalogQuery(CatalogKinds.Skills, "  pyth ", 5, null), CancellationToken.None);

        Assert.Equal("PYTH", reader.LastSearch);
        Assert.Equal(6, reader.LastTake);
        Assert.Null(reader.LastAfter);
    }

    [Fact]
    public async Task List_LastPage_HasNoNextCursor()
    {
        reader.Entries = [Entry("A"), Entry("B")];

        var page = await ListPageAsync(new ListCatalogQuery(CatalogKinds.Cities, null, 2, null));

        Assert.Equal(2, page.Items.Count);
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public async Task List_NextCursor_ContinuesAfterLastReturnedEntry()
    {
        var last = Entry("B", sortOrder: 3);
        reader.Entries = [Entry("A"), last, Entry("C")];

        var first = await ListPageAsync(new ListCatalogQuery(CatalogKinds.Formats, "о", 2, null));

        Assert.Equal(2, first.Items.Count);
        Assert.NotNull(first.NextCursor);

        await ListPageAsync(new ListCatalogQuery(CatalogKinds.Formats, "о", 2, first.NextCursor));

        Assert.Equal(last, reader.LastAfter);
    }

    [Theory]
    [InlineData("not-base64!")]
    [InlineData("e30")]
    [InlineData("bnVsbA")]
    public async Task List_DamagedCursor_ReturnsInvalidCursor(string cursor)
    {
        var result = await service.ListAsync(new ListCatalogQuery(CatalogKinds.Skills, null, 20, cursor), CancellationToken.None);

        Assert.IsType<ListCatalogResult.InvalidCursor>(result);
    }

    [Fact]
    public async Task List_CursorFromOtherKindOrSearch_ReturnsInvalidCursor()
    {
        reader.Entries = [Entry("A"), Entry("B")];
        var page = await ListPageAsync(new ListCatalogQuery(CatalogKinds.Skills, "a", 1, null));

        var otherKind = await service.ListAsync(new ListCatalogQuery(CatalogKinds.Cities, "a", 1, page.NextCursor), CancellationToken.None);
        var otherSearch = await service.ListAsync(new ListCatalogQuery(CatalogKinds.Skills, "b", 1, page.NextCursor), CancellationToken.None);

        Assert.IsType<ListCatalogResult.InvalidCursor>(otherKind);
        Assert.IsType<ListCatalogResult.InvalidCursor>(otherSearch);
    }

    private async Task<CatalogPage> ListPageAsync(ListCatalogQuery query)
    {
        var result = await service.ListAsync(query, CancellationToken.None);
        return Assert.IsType<ListCatalogResult.Found>(result).Page;
    }

    private static CatalogEntry Entry(string name, short sortOrder = 0) => new(Guid.CreateVersion7(), name, sortOrder);

    private sealed class FakeCatalogReader : ICatalogReader
    {
        public IReadOnlyList<CatalogEntry> Entries { get; set; } = [];

        public string? LastKind { get; private set; }

        public string? LastSearch { get; private set; }

        public CatalogEntry? LastAfter { get; private set; }

        public int LastTake { get; private set; }

        public Task<IReadOnlyList<CatalogEntry>> ListAsync(
            string kind,
            string? normalizedSearch,
            CatalogEntry? after,
            int take,
            CancellationToken cancellationToken)
        {
            LastKind = kind;
            LastSearch = normalizedSearch;
            LastAfter = after;
            LastTake = take;

            return Task.FromResult<IReadOnlyList<CatalogEntry>>(Entries.Take(take).ToList());
        }
    }
}
