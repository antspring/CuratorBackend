using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Catalogs;
using Domain.Users;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Mentees;

public sealed record PersonalInput(string FirstName, string LastName, int Age, Guid CityId, Guid LanguageId, Guid? PhotoFileId);

public sealed record MenteeProfileInput(
    PersonalInput Personal,
    Guid[] SkillIds,
    Guid SkillLevelId,
    Guid[] TaskIds,
    Guid FormatId,
    Guid AvailabilityId,
    string? BudgetFrom,
    string? BudgetTo,
    string Goal,
    Guid GoalDeadlineId);

public sealed record PersonalDto(
    string FirstName,
    string LastName,
    int Age,
    Guid CityId,
    Guid LanguageId,
    Guid? PhotoFileId,
    string? PhotoUrl);

public sealed record MenteeProfileDto(
    Guid UserId,
    PersonalDto Personal,
    Guid[] SkillIds,
    Guid SkillLevelId,
    Guid[] TaskIds,
    Guid FormatId,
    Guid AvailabilityId,
    string? BudgetFrom,
    string? BudgetTo,
    string Goal,
    Guid GoalDeadlineId,
    DateTimeOffset UpdatedAt);

public class MenteeProfileTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Path = "/api/v1/me/mentee-profile";
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    [Fact]
    public async Task Get_BeforeSave_ReturnsNotFound()
    {
        var (client, _) = await SignedInClientAsync();

        var response = await client.GetAsync(Path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("resource_not_found", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Put_WithoutToken_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().PutAsJsonAsync(Path, await ValidProfileAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_FirstSave_ReturnsProfileAndAssignsMenteeRole()
    {
        var (client, tokens) = await SignedInClientAsync();
        var input = await ValidProfileAsync() with { BudgetFrom = "1500.00", BudgetTo = "3000.50" };

        var response = await client.PutAsJsonAsync(Path, input);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.ReadAsync<MenteeProfileDto>();
        Assert.Equal(tokens.Account.Id, saved.UserId);
        Assert.Equal(
            new PersonalDto("Анна", "Иванова", 21, input.Personal.CityId, input.Personal.LanguageId, null, null),
            saved.Personal);
        Assert.Equal(input.SkillIds.Order(), saved.SkillIds);
        Assert.Equal(input.TaskIds.Order(), saved.TaskIds);
        Assert.Equal("1500.00", saved.BudgetFrom);
        Assert.Equal("3000.50", saved.BudgetTo);
        Assert.Equal(input.Goal, saved.Goal);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.EndsWith("Z", json.RootElement.GetProperty("updatedAt").GetString());

        var refreshed = await client.RefreshAsync(tokens.RefreshToken);
        Assert.Equal(["mentee"], (await refreshed.ReadAsync<TokenPairDto>()).Account.Roles);

        var fetched = await (await client.GetAsync(Path)).ReadAsync<MenteeProfileDto>();
        Assert.Equal(saved.SkillIds, fetched.SkillIds);
        Assert.Equal(saved.UpdatedAt, fetched.UpdatedAt);
    }

    [Fact]
    public async Task Put_Again_ReplacesWholeProfile()
    {
        var (client, _) = await SignedInClientAsync();
        var catalogs = await CatalogsAsync();
        var first = await ValidProfileAsync() with { BudgetFrom = "100.00" };
        (await client.PutAsJsonAsync(Path, first)).EnsureSuccessStatusCode();
        var photoId = await UploadAvatarAsync(client);

        var second = first with
        {
            Personal = first.Personal with { FirstName = "Мария", PhotoFileId = photoId },
            SkillIds = [catalogs[CatalogKinds.Skills][1], catalogs[CatalogKinds.Skills][2]],
            TaskIds = [catalogs[CatalogKinds.MenteeTasks][1]],
            BudgetFrom = null,
        };

        var response = await client.PutAsJsonAsync(Path, second);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await (await client.GetAsync(Path)).ReadAsync<MenteeProfileDto>();
        Assert.Equal("Мария", fetched.Personal.FirstName);
        Assert.Equal(photoId, fetched.Personal.PhotoFileId);
        Assert.StartsWith($"{factory.StorageUrl}/{ApiFactory.StorageBucket}/avatars/", fetched.Personal.PhotoUrl);
        Assert.Equal(second.SkillIds.Order(), fetched.SkillIds);
        Assert.Equal(second.TaskIds, fetched.TaskIds);
        Assert.Null(fetched.BudgetFrom);
    }

    [Theory]
    [InlineData(Roles.Mentor, HttpStatusCode.Conflict, "role_conflict")]
    [InlineData(Roles.Curator, HttpStatusCode.Forbidden, "forbidden")]
    public async Task Put_ForOtherRole_IsRejected(string role, HttpStatusCode status, string code)
    {
        var (client, tokens) = await SignedInClientAsync();
        await AddRoleAsync(tokens.Account.Id, role);

        var response = await client.PutAsJsonAsync(Path, await ValidProfileAsync());

        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await response.ReadAsync<ProblemDto>()).Code);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(Path)).StatusCode);
    }

    [Fact]
    public async Task Put_LegacyUserWithBothRoles_IsAllowed()
    {
        var (client, tokens) = await SignedInClientAsync();
        await AddRoleAsync(tokens.Account.Id, Roles.Mentor);
        await AddRoleAsync(tokens.Account.Id, Roles.Mentee);

        var response = await client.PutAsJsonAsync(Path, await ValidProfileAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("personal.languageId")]
    [InlineData("personal.cityId")]
    [InlineData("skillIds")]
    [InlineData("formatId")]
    public async Task Put_IdFromOtherCatalog_ReturnsFieldError(string field)
    {
        var (client, _) = await SignedInClientAsync();
        var valid = await ValidProfileAsync();
        var wrongId = valid.AvailabilityId;

        var input = field switch
        {
            "personal.languageId" => valid with { Personal = valid.Personal with { LanguageId = wrongId } },
            "personal.cityId" => valid with { Personal = valid.Personal with { CityId = wrongId } },
            "skillIds" => valid with { SkillIds = [valid.SkillIds[0], wrongId] },
            _ => valid with { FormatId = Guid.NewGuid() },
        };

        var response = await client.PutAsJsonAsync(Path, input);

        await AssertFieldErrorAsync(response, field);
    }

    [Fact]
    public async Task Put_SomeoneElsesPhoto_ReturnsFieldError()
    {
        var (stranger, _) = await SignedInClientAsync();
        var strangerPhotoId = await UploadAvatarAsync(stranger);
        var (client, _) = await SignedInClientAsync();
        var valid = await ValidProfileAsync();

        var response = await client.PutAsJsonAsync(Path, valid with { Personal = valid.Personal with { PhotoFileId = strangerPhotoId } });

        await AssertFieldErrorAsync(response, "personal.photoFileId");
    }

    [Fact]
    public async Task Put_DuplicateSkills_ReturnsFieldError()
    {
        var (client, _) = await SignedInClientAsync();
        var valid = await ValidProfileAsync();

        var response = await client.PutAsJsonAsync(Path, valid with { SkillIds = [valid.SkillIds[0], valid.SkillIds[0]] });

        await AssertFieldErrorAsync(response, "skillIds");
    }

    [Theory]
    [InlineData("1500", null, "budgetFrom")]
    [InlineData("2000.00", "1000.00", "budgetTo")]
    public async Task Put_InvalidBudget_ReturnsFieldError(string? budgetFrom, string? budgetTo, string field)
    {
        var (client, _) = await SignedInClientAsync();
        var valid = await ValidProfileAsync();

        var response = await client.PutAsJsonAsync(Path, valid with { BudgetFrom = budgetFrom, BudgetTo = budgetTo });

        await AssertFieldErrorAsync(response, field);
    }

    [Fact]
    public async Task Put_MissingPersonalFieldAndShortGoal_ReturnsFieldErrors()
    {
        var (client, _) = await SignedInClientAsync();
        var valid = await ValidProfileAsync();

        var response = await client.PutAsJsonAsync(Path, valid with
        {
            Personal = valid.Personal with { LastName = null! },
            Goal = "Коротко",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Contains("personal.lastName", problem.Errors!.Keys);
        Assert.Contains("goal", problem.Errors.Keys);
    }

    private static async Task AssertFieldErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal("validation_failed", problem.Code);
        Assert.Contains(field, problem.Errors!.Keys);
    }

    private async Task<(HttpClient Client, TokenPairDto Tokens)> SignedInClientAsync()
    {
        var client = factory.CreateClient();
        var tokens = await client.RegisterAndLoginAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        return (client, tokens);
    }

    private async Task<MenteeProfileInput> ValidProfileAsync()
    {
        var catalogs = await CatalogsAsync();

        return new MenteeProfileInput(
            new PersonalInput("Анна", "Иванова", 21, catalogs[CatalogKinds.Cities][0], catalogs[CatalogKinds.Languages][0], null),
            [catalogs[CatalogKinds.Skills][0], catalogs[CatalogKinds.Skills][1]],
            catalogs[CatalogKinds.SkillLevels][0],
            [catalogs[CatalogKinds.MenteeTasks][0]],
            catalogs[CatalogKinds.Formats][0],
            catalogs[CatalogKinds.Availability][0],
            null,
            "2000.00",
            "Освоить ASP.NET Core и собрать учебный API",
            catalogs[CatalogKinds.GoalDeadlines][0]);
    }

    private async Task<Dictionary<string, List<Guid>>> CatalogsAsync()
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

    private async Task AddRoleAsync(Guid userId, string role)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByIdAsync(userId.ToString());

        Assert.True((await users.AddToRoleAsync(user!, role)).Succeeded);
    }

    private static async Task<Guid> UploadAvatarAsync(HttpClient client)
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
}
