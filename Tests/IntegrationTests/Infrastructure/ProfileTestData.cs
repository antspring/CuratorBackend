using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Domain.Catalogs;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Infrastructure;

public sealed record PersonalInput(string FirstName, string LastName, int Age, Guid CityId, Guid LanguageId, Guid? PhotoFileId);

public sealed record PersonalDto(
    string FirstName,
    string LastName,
    int Age,
    Guid CityId,
    Guid LanguageId,
    Guid? PhotoFileId,
    string? PhotoUrl);

public static class ProfileTestData
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    public static PersonalInput Personal(Dictionary<string, List<Guid>> catalogs) =>
        new("Анна", "Иванова", 21, catalogs[CatalogKinds.Cities][0], catalogs[CatalogKinds.Languages][0], null);

    public static async Task<(HttpClient Client, TokenPairDto Tokens)> SignedInClientAsync(this ApiFactory factory)
    {
        var client = factory.CreateClient();
        var tokens = await client.RegisterAndLoginAsync(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        return (client, tokens);
    }

    public static async Task<Dictionary<string, List<Guid>>> CatalogsAsync(this ApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var catalogs = await db.CatalogItems
            .OrderBy(item => item.SortOrder)
            .GroupBy(item => item.Kind)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.Id).ToList());

        catalogs[CatalogKinds.Skills] = await db.Skills.OrderBy(skill => skill.Code).Select(skill => skill.Id).ToListAsync();
        catalogs[CatalogKinds.Cities] = await db.Cities.OrderBy(city => city.Code).Select(city => city.Id).ToListAsync();

        return catalogs;
    }

    public static async Task AddRoleAsync(this ApiFactory factory, Guid userId, string role)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByIdAsync(userId.ToString());

        Assert.True((await users.AddToRoleAsync(user!, role)).Succeeded);
    }

    public static async Task<Guid> UploadAvatarAsync(HttpClient client)
    {
        var file = new ByteArrayContent(Png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var response = await client.PostAsync("/api/v1/files", new MultipartFormDataContent
        {
            { new StringContent("avatar"), "purpose" },
            { file, "file", "me.png" },
        });

        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return json.RootElement.GetProperty("id").GetGuid();
    }

    public static async Task AssertFieldErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal("validation_failed", problem.Code);
        Assert.Contains(field, problem.Errors!.Keys);
    }
}
