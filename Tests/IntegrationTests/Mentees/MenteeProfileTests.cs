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

public sealed record OwnedSkillDto(Guid SkillId, Guid SkillLevelId);

public sealed record MenteeProfileDto(
    Guid UserId,
    PersonalDto Personal,
    OwnedSkillDto[] OwnedSkills,
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

    [Fact]
    public async Task PutPersonal_PreservesQuestionnaireAndSurvivesQuestionnaireUpdates()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var input = await ValidProfileAsync();
        var initial = await client.PutAsJsonAsync(Path, input);
        initial.EnsureSuccessStatusCode();
        var initialJson = await initial.ReadAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, initialJson.GetProperty("personal").GetProperty("university").ValueKind);

        var photoId = await ProfileTestData.UploadAvatarAsync(client);
        var details = PersonalDetails(input) with { PhotoFileId = photoId };
        var update = await client.PutAsJsonAsync($"{Path}/personal", details);
        update.EnsureSuccessStatusCode();
        var saved = await update.ReadAsync<JsonElement>();
        Assert.Equal("Мария", saved.GetProperty("firstName").GetString());
        Assert.Equal(photoId, saved.GetProperty("photoFileId").GetGuid());
        Assert.NotNull(saved.GetProperty("photoUrl").GetString());
        Assert.Equal(input.Personal.LanguageId, saved.GetProperty("languageId").GetGuid());
        Assert.Equal("Студентка", saved.GetProperty("status").GetString());

        var after = await client.GetFromJsonAsync<MenteeProfileDto>(Path);
        Assert.Equal(input.Goal, after!.Goal);
        Assert.Equal(input.SkillIds.Order(), after.SkillIds.Order());
        Assert.Equal(input.BudgetTo, after.BudgetTo);

        (await client.PutAsJsonAsync(Path, input)).EnsureSuccessStatusCode();
        var profile = await client.GetFromJsonAsync<JsonElement>(Path);
        var personal = profile.GetProperty("personal");
        Assert.Equal(input.Personal.FirstName, personal.GetProperty("firstName").GetString());
        Assert.Equal("Студентка", personal.GetProperty("status").GetString());
        Assert.Equal(details.University, personal.GetProperty("university").GetString());
        Assert.Equal(details.Specialty, personal.GetProperty("specialty").GetString());
        Assert.Equal(details.AboutMe, personal.GetProperty("aboutMe").GetString());

        var cleared = await client.PutAsJsonAsync($"{Path}/personal", details with { Specialty = null, AboutMe = null });
        cleared.EnsureSuccessStatusCode();
        var clearedJson = await cleared.ReadAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, clearedJson.GetProperty("specialty").ValueKind);
        Assert.Equal(JsonValueKind.Null, clearedJson.GetProperty("aboutMe").ValueKind);
    }

    [Fact]
    public async Task PutPersonal_RequiresAuthenticationAndAnExistingMenteeProfile()
    {
        var input = PersonalDetails(await ValidProfileAsync());
        var path = $"{Path}/personal";
        var anonymous = await factory.CreateClient().PutAsJsonAsync(path, input);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var (client, tokens) = await factory.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(path, input)).StatusCode);
        Assert.Empty((await (await client.RefreshAsync(tokens.RefreshToken)).ReadAsync<TokenPairDto>()).Account.Roles);
        await factory.AddRoleAsync(tokens.Account.Id, Roles.Mentor);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(path, input)).StatusCode);
    }

    [Fact]
    public async Task PutPersonal_InvalidFieldsAndForeignAvatarLeaveProfileIntact()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var input = await ValidProfileAsync();
        (await client.PutAsJsonAsync(Path, input)).EnsureSuccessStatusCode();
        var details = PersonalDetails(input);
        var (other, _) = await factory.SignedInClientAsync();
        var foreignPhoto = await ProfileTestData.UploadAvatarAsync(other);
        var path = $"{Path}/personal";

        await ProfileTestData.AssertFieldErrorAsync(await client.PutAsJsonAsync(path, details with { PhotoFileId = foreignPhoto }), "photoFileId");
        await ProfileTestData.AssertFieldErrorAsync(await client.PutAsJsonAsync(path, details with { CityId = input.SkillLevelId }), "cityId");
        var invalid = await client.PutAsJsonAsync(path, details with { Status = " ", University = "", AboutMe = new string('а', 301) });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var errors = (await invalid.ReadAsync<ProblemDto>()).Errors!;
        Assert.Contains("status", errors.Keys);
        Assert.Contains("university", errors.Keys);
        Assert.Contains("aboutMe", errors.Keys);
        var personal = (await client.GetFromJsonAsync<JsonElement>(Path)).GetProperty("personal");
        Assert.Equal(input.Personal.FirstName, personal.GetProperty("firstName").GetString());
        Assert.Equal(JsonValueKind.Null, personal.GetProperty("status").ValueKind);
    }

    [Fact]
    public async Task PutSkills_ReplacesOwnSkillsAndPreservesQuestionnaire()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var input = await ValidProfileAsync();
        var initial = await client.PutAsJsonAsync(Path, input);
        initial.EnsureSuccessStatusCode();
        Assert.Empty((await initial.ReadAsync<MenteeProfileDto>()).OwnedSkills);

        var catalogs = await factory.CatalogsAsync();
        var skills = catalogs[CatalogKinds.Skills];
        var levels = catalogs[CatalogKinds.SkillLevels];
        var first = new OwnedSkillDto(skills[0], levels[0]);
        var second = new OwnedSkillDto(skills[2], levels[1]);
        var updated = first with { SkillLevelId = levels[2] };
        var skillsPath = $"{Path}/skills";

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(skillsPath, new { ownedSkills = new[] { first, second } })).StatusCode);
        var saved = await client.GetFromJsonAsync<MenteeProfileDto>(Path);
        Assert.Equal(new[] { first, second }.OrderBy(skill => skill.SkillId), saved!.OwnedSkills);
        Assert.Equal(input.SkillIds.Order(), saved.SkillIds);
        Assert.Equal(input.SkillLevelId, saved.SkillLevelId);

        var details = PersonalDetails(input);
        (await client.PutAsJsonAsync($"{Path}/personal", details)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(skillsPath, new { ownedSkills = new[] { updated } })).StatusCode);
        var replaced = await client.GetFromJsonAsync<MenteeProfileDto>(Path);
        Assert.Equal(updated, Assert.Single(replaced!.OwnedSkills));
        var personal = (await client.GetFromJsonAsync<JsonElement>(Path)).GetProperty("personal");
        Assert.Equal(details.University, personal.GetProperty("university").GetString());

        var questionnaire = await client.PutAsJsonAsync(Path, input with { SkillIds = [skills[1]] });
        questionnaire.EnsureSuccessStatusCode();
        var afterQuestionnaire = await questionnaire.ReadAsync<MenteeProfileDto>();
        Assert.Equal(new[] { skills[1] }, afterQuestionnaire.SkillIds);
        Assert.Equal(updated, Assert.Single(afterQuestionnaire.OwnedSkills));

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(skillsPath, new { ownedSkills = Array.Empty<OwnedSkillDto>() })).StatusCode);
        var cleared = await client.GetFromJsonAsync<MenteeProfileDto>(Path);
        Assert.Empty(cleared!.OwnedSkills);
        Assert.Equal(afterQuestionnaire.SkillIds, cleared.SkillIds);
    }

    [Fact]
    public async Task PutSkills_InvalidEntriesDoNotChangeSavedSkills()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var input = await ValidProfileAsync();
        (await client.PutAsJsonAsync(Path, input)).EnsureSuccessStatusCode();
        var skill = new OwnedSkillDto(input.SkillIds[0], input.SkillLevelId);
        var skillsPath = $"{Path}/skills";
        (await client.PutAsJsonAsync(skillsPath, new { ownedSkills = new[] { skill } })).EnsureSuccessStatusCode();

        var wrongLevel = skill with { SkillLevelId = input.FormatId };
        var wrongSkill = skill with { SkillId = input.SkillLevelId };
        OwnedSkillDto?[][] invalidLists = [[skill, skill], [wrongLevel], [wrongSkill], [null]];
        foreach (var invalid in invalidLists)
        {
            var response = await client.PutAsJsonAsync(skillsPath, new { ownedSkills = invalid });
            await ProfileTestData.AssertFieldErrorAsync(response, "ownedSkills");
        }

        var saved = await client.GetFromJsonAsync<MenteeProfileDto>(Path);
        Assert.Equal(skill, Assert.Single(saved!.OwnedSkills));
    }

    [Fact]
    public async Task PutSkills_RequiresAuthenticationAndAnExistingMenteeProfile()
    {
        var skillsPath = $"{Path}/skills";
        var body = new { ownedSkills = Array.Empty<OwnedSkillDto>() };
        var anonymous = await factory.CreateClient().PutAsJsonAsync(skillsPath, body);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var (client, _) = await factory.SignedInClientAsync();
        var missing = await client.PutAsJsonAsync(skillsPath, body);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("resource_not_found", (await missing.ReadAsync<ProblemDto>()).Code);
    }

    private sealed record PersonalDetailsInput(string FirstName, string LastName, int Age, Guid CityId, Guid? PhotoFileId,
        string Status, string University, string? Specialty, string? AboutMe);

    private static PersonalDetailsInput PersonalDetails(MenteeProfileInput input) =>
        new("Мария", "Сидорова", 20, input.Personal.CityId, null, " Студентка ", "УрФУ", "Прикладная информатика", "Изучаю анализ данных.");

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
