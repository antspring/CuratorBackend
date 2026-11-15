using System.Net;
using System.Net.Http.Headers;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Catalogs;

public sealed record LookupDto(Guid Id, string Name);

public sealed record LookupPageDto(LookupDto[] Items, string? NextCursor);

public class CatalogEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly HttpClient client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        var tokens = await client.RegisterAndLoginAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync("/api/v1/catalogs/skills");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_CatalogItems_AreOrderedBySortOrder()
    {
        var page = await GetPageAsync("/api/v1/catalogs/skill-levels");

        Assert.Equal(["Базовый", "Уверенный", "Продвинутый", "Эксперт"], page.Items.Select(item => item.Name));
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public async Task List_Skills_SearchesCaseInsensitively()
    {
        var page = await GetPageAsync("/api/v1/catalogs/skills?q=SCRIPT");

        Assert.Equal(["JavaScript", "TypeScript"], page.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task List_CatalogItems_SearchesCaseInsensitively()
    {
        var page = await GetPageAsync("/api/v1/catalogs/positions?q=РАЗРАБОТЧИК");

        Assert.Equal(4, page.Items.Length);
        Assert.All(page.Items, item => Assert.Contains("разработчик", item.Name));
    }

    [Fact]
    public async Task List_Cities_PagesThroughAllEntriesOrderedByName()
    {
        var names = new List<string>();
        string? cursor = null;
        var pages = 0;

        do
        {
            var url = "/api/v1/catalogs/cities?limit=7" + (cursor is null ? "" : $"&cursor={cursor}");
            var page = await GetPageAsync(url);
            names.AddRange(page.Items.Select(item => item.Name));
            cursor = page.NextCursor;
            pages++;
        }
        while (cursor is not null);

        Assert.Equal(30, names.Count);
        Assert.Equal(5, pages);
        Assert.Equal(names.Count, names.Distinct().Count());
        Assert.Contains("Екатеринбург", names);
    }

    [Fact]
    public async Task List_UnknownKind_ReturnsNotFound()
    {
        var response = await client.GetAsync("/api/v1/catalogs/interests");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("resource_not_found", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task List_CursorWithOtherSearch_ReturnsInvalidCursor()
    {
        var first = await GetPageAsync("/api/v1/catalogs/skills?limit=1");

        var response = await client.GetAsync($"/api/v1/catalogs/skills?limit=1&q=java&cursor={first.NextCursor}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_cursor", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Theory]
    [InlineData("limit=0")]
    [InlineData("limit=101")]
    public async Task List_LimitOutOfRange_ReturnsValidationError(string query)
    {
        var response = await client.GetAsync($"/api/v1/catalogs/skills?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal("validation_failed", problem.Code);
        Assert.True(problem.Errors?.ContainsKey("limit"));
    }

    private async Task<LookupPageDto> GetPageAsync(string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<LookupPageDto>();
    }
}
