using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Catalogs;
using Domain.Users;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Mentees;

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

    [Fact]
    public async Task Get_BeforeSave_ReturnsNotFound()
    {
        var (client, _) = await factory.SignedInClientAsync();

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
        var (client, tokens) = await factory.SignedInClientAsync();
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
        var (client, _) = await factory.SignedInClientAsync();
        var catalogs = await factory.CatalogsAsync();
        var first = await ValidProfileAsync() with { BudgetFrom = "100.00" };
        (await client.PutAsJsonAsync(Path, first)).EnsureSuccessStatusCode();
        var photoId = await ProfileTestData.UploadAvatarAsync(client);

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
        var (client, tokens) = await factory.SignedInClientAsync();
        await factory.AddRoleAsync(tokens.Account.Id, role);

        var response = await client.PutAsJsonAsync(Path, await ValidProfileAsync());

        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await response.ReadAsync<ProblemDto>()).Code);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(Path)).StatusCode);
    }

    [Fact]
    public async Task Put_LegacyUserWithBothRoles_IsAllowed()
    {
        var (client, tokens) = await factory.SignedInClientAsync();
        await factory.AddRoleAsync(tokens.Account.Id, Roles.Mentor);
        await factory.AddRoleAsync(tokens.Account.Id, Roles.Mentee);

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
        var (client, _) = await factory.SignedInClientAsync();
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

        await ProfileTestData.AssertFieldErrorAsync(response, field);
    }

    [Fact]
    public async Task Put_SomeoneElsesPhoto_ReturnsFieldError()
    {
        var (stranger, _) = await factory.SignedInClientAsync();
        var strangerPhotoId = await ProfileTestData.UploadAvatarAsync(stranger);
        var (client, _) = await factory.SignedInClientAsync();
        var valid = await ValidProfileAsync();

        var response = await client.PutAsJsonAsync(Path, valid with { Personal = valid.Personal with { PhotoFileId = strangerPhotoId } });

        await ProfileTestData.AssertFieldErrorAsync(response, "personal.photoFileId");
    }

    [Fact]
    public async Task Put_DuplicateSkills_ReturnsFieldError()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var valid = await ValidProfileAsync();

        var response = await client.PutAsJsonAsync(Path, valid with { SkillIds = [valid.SkillIds[0], valid.SkillIds[0]] });

        await ProfileTestData.AssertFieldErrorAsync(response, "skillIds");
    }

    [Theory]
    [InlineData("1500", null, "budgetFrom")]
    [InlineData("2000.00", "1000.00", "budgetTo")]
    public async Task Put_InvalidBudget_ReturnsFieldError(string? budgetFrom, string? budgetTo, string field)
    {
        var (client, _) = await factory.SignedInClientAsync();
        var valid = await ValidProfileAsync();

        var response = await client.PutAsJsonAsync(Path, valid with { BudgetFrom = budgetFrom, BudgetTo = budgetTo });

        await ProfileTestData.AssertFieldErrorAsync(response, field);
    }

    [Fact]
    public async Task Put_MissingPersonalFieldAndShortGoal_ReturnsFieldErrors()
    {
        var (client, _) = await factory.SignedInClientAsync();
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

    private async Task<MenteeProfileInput> ValidProfileAsync()
    {
        var catalogs = await factory.CatalogsAsync();

        return new MenteeProfileInput(
            ProfileTestData.Personal(catalogs),
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
}
