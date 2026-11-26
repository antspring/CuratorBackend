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

public sealed record ProfileGoalDto(Guid Id, string Text, Guid DeadlineId, Guid[] DirectionIds);

public sealed record ProfileLookupDto(Guid Id, string Name);

public sealed record MenteePersonalDto(
    string FirstName, string LastName, int Age, ProfileLookupDto City, ProfileLookupDto Language,
    Guid? PhotoFileId, string? PhotoUrl);

public sealed record MenteeOwnedSkillDto(ProfileLookupDto Skill, ProfileLookupDto Level)
{
    public OwnedSkillDto ToInput() => new(Skill.Id, Level.Id);
}

public sealed record MenteeGoalDto(Guid Id, string Text, ProfileLookupDto Deadline, ProfileLookupDto[] Directions)
{
    public ProfileGoalDto ToInput() => new(Id, Text, Deadline.Id, Directions.Select(direction => direction.Id).ToArray());
}

public sealed record MenteeProfileDto(
    Guid UserId,
    MenteePersonalDto Personal,
    MenteeOwnedSkillDto[] OwnedSkills,
    ProfileLookupDto[] Skills,
    ProfileLookupDto SkillLevel,
    ProfileLookupDto[] Tasks,
    string? BudgetFrom,
    string? BudgetTo,
    string Goal,
    MenteeGoalDto[] Goals,
    DateTimeOffset UpdatedAt);

public class MenteeProfileTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Path = "/api/v1/me/mentee-profile";

    [Fact]
    public async Task MentorCard_ForAnotherMenteeWithAvatar_ReturnsNotFound()
    {
        var (owner, ownerTokens) = await factory.SignedInClientAsync();
        var (viewer, _) = await factory.SignedInClientAsync();
        var input = await ValidProfileAsync();
        var avatar = await ProfileTestData.UploadAvatarAsync(owner);
        var withPhoto = input with { Personal = input.Personal with { PhotoFileId = avatar } };
        (await owner.PutAsJsonAsync(Path, withPhoto)).EnsureSuccessStatusCode();
        (await viewer.PutAsJsonAsync(Path, input)).EnsureSuccessStatusCode();

        var response = await viewer.GetAsync($"/api/v1/mentors/{ownerTokens.Account.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("resource_not_found", (await response.ReadAsync<ProblemDto>()).Code);
    }

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
            ("Анна", "Иванова", 21, input.Personal.CityId, input.Personal.LanguageId),
            (saved.Personal.FirstName, saved.Personal.LastName, saved.Personal.Age, saved.Personal.City.Id, saved.Personal.Language.Id));
        Assert.Null(saved.Personal.PhotoFileId);
        Assert.Null(saved.Personal.PhotoUrl);
        Assert.Equal(input.SkillIds.Order(), saved.Skills.Select(skill => skill.Id));
        Assert.Equal(input.TaskIds.Order(), saved.Tasks.Select(task => task.Id));
        Assert.Equal("1500.00", saved.BudgetFrom);
        Assert.Equal("3000.50", saved.BudgetTo);
        Assert.Equal(input.Goal, saved.Goal);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.EndsWith("Z", json.RootElement.GetProperty("updatedAt").GetString());

        var refreshed = await client.RefreshAsync(tokens.RefreshToken);
        Assert.Equal(["mentee"], (await refreshed.ReadAsync<TokenPairDto>()).Account.Roles);

        var fetched = await (await client.GetAsync(Path)).ReadAsync<MenteeProfileDto>();
        Assert.Equal(saved.Skills.Select(skill => skill.Id), fetched.Skills.Select(skill => skill.Id));
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
        Assert.Equal(second.SkillIds.Order(), fetched.Skills.Select(skill => skill.Id));
        Assert.Equal(second.TaskIds, fetched.Tasks.Select(task => task.Id));
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
        Assert.Equal(input.Personal.LanguageId, saved.GetProperty("language").GetProperty("id").GetGuid());
        Assert.Equal("Студентка", saved.GetProperty("status").GetString());

        var after = await client.GetFromJsonAsync<MenteeProfileDto>(Path);
        Assert.Equal(input.Goal, after!.Goal);
        Assert.Equal(input.SkillIds.Order(), after.Skills.Select(skill => skill.Id).Order());
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
        Assert.Equal(new[] { first, second }.OrderBy(skill => skill.SkillId), saved!.OwnedSkills.Select(skill => skill.ToInput()));
        Assert.Equal(input.SkillIds.Order(), saved.Skills.Select(skill => skill.Id));
        Assert.Equal(input.SkillLevelId, saved.SkillLevel.Id);

        var details = PersonalDetails(input);
        (await client.PutAsJsonAsync($"{Path}/personal", details)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(skillsPath, new { ownedSkills = new[] { updated } })).StatusCode);
        var replaced = await client.GetFromJsonAsync<MenteeProfileDto>(Path);
        Assert.Equal(updated, Assert.Single(replaced!.OwnedSkills).ToInput());
        var personal = (await client.GetFromJsonAsync<JsonElement>(Path)).GetProperty("personal");
        Assert.Equal(details.University, personal.GetProperty("university").GetString());

        var questionnaire = await client.PutAsJsonAsync(Path, input with { SkillIds = [skills[1]] });
        questionnaire.EnsureSuccessStatusCode();
        var afterQuestionnaire = await questionnaire.ReadAsync<MenteeProfileDto>();
        Assert.Equal(new[] { skills[1] }, afterQuestionnaire.Skills.Select(skill => skill.Id));
        Assert.Equal(updated, Assert.Single(afterQuestionnaire.OwnedSkills).ToInput());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(skillsPath, new { ownedSkills = Array.Empty<OwnedSkillDto>() })).StatusCode);
        var cleared = await client.GetFromJsonAsync<MenteeProfileDto>(Path);
        Assert.Empty(cleared!.OwnedSkills);
        Assert.Equal(afterQuestionnaire.Skills.Select(skill => skill.Id), cleared.Skills.Select(skill => skill.Id));
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
        Assert.Equal(skill, Assert.Single(saved!.OwnedSkills).ToInput());
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

    [Fact]
    public async Task PutGoals_SeedsQuestionnaireGoalOnceAndPreservesEditedList()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var input = await ValidProfileAsync();
        var saved = await client.PutAsJsonAsync(Path, input);
        saved.EnsureSuccessStatusCode();
        var imported = Assert.Single((await saved.ReadAsync<MenteeProfileDto>()).Goals).ToInput();
        Assert.NotEqual(Guid.Empty, imported.Id);
        Assert.Equal(input.Goal, imported.Text);
        Assert.Equal(input.GoalDeadlineId, imported.DeadlineId);
        Assert.Empty(imported.DirectionIds);

        var revisedInput = input with { Goal = "Другая цель из повторно заполненной анкеты" };
        var questionnaire = await client.PutAsJsonAsync(Path, revisedInput);
        questionnaire.EnsureSuccessStatusCode();
        var unchanged = Assert.Single((await questionnaire.ReadAsync<MenteeProfileDto>()).Goals);
        Assert.Equal(imported.Id, unchanged.Id);
        Assert.Equal(imported.Text, unchanged.Text);

        var catalogs = await factory.CatalogsAsync();
        var direction = catalogs[CatalogKinds.MentoringDirections][0];
        var newGoal = new ProfileGoalDto(Guid.NewGuid(), "Подготовить проект для собеседования",
            catalogs[CatalogKinds.GoalDeadlines][1], [direction]);
        var goalsPath = $"{Path}/goals";
        var initialList = new[] { imported, newGoal };
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(goalsPath, new { goals = initialList })).StatusCode);

        var edited = imported with { Text = "Улучшить навыки серверной разработки", DirectionIds = [direction] };
        var choices = new[] { newGoal, edited };
        for (var retry = 0; retry < 2; retry++)
        {
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(goalsPath, new { goals = choices })).StatusCode);
        }

        var profile = await client.GetFromJsonAsync<MenteeProfileDto>(Path);
        Assert.Equal(choices.Select(goal => (goal.Id, goal.Text, goal.DeadlineId)),
            profile!.Goals.Select(goal => (goal.Id, goal.Text, goal.Deadline.Id)));
        Assert.All(profile.Goals, goal => Assert.Equal(direction, Assert.Single(goal.Directions).Id));
        Assert.Equal(revisedInput.Goal, profile.Goal);
        Assert.Equal(input.SkillIds.Order(), profile.Skills.Select(skill => skill.Id));

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(goalsPath, new { goals = Array.Empty<ProfileGoalDto>() })).StatusCode);
        var repeated = await client.PutAsJsonAsync(Path, revisedInput);
        repeated.EnsureSuccessStatusCode();
        Assert.Empty((await repeated.ReadAsync<MenteeProfileDto>()).Goals);
    }

    [Fact]
    public async Task PutGoals_InvalidDirectionsAndDuplicateIdsLeaveSavedListIntact()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var input = await ValidProfileAsync();
        var saved = await client.PutAsJsonAsync(Path, input);
        saved.EnsureSuccessStatusCode();
        var original = Assert.Single((await saved.ReadAsync<MenteeProfileDto>()).Goals).ToInput();
        var goalsPath = $"{Path}/goals";

        var wrongDirection = original with { DirectionIds = [input.TaskIds[0]] };
        var missingDirections = original with { Id = Guid.NewGuid() };
        var wrongDeadline = original with { DeadlineId = input.FormatId };
        await ProfileTestData.AssertFieldErrorAsync(
            await client.PutAsJsonAsync(goalsPath, new { goals = new[] { wrongDirection } }), "goals[0].directionIds");
        await ProfileTestData.AssertFieldErrorAsync(
            await client.PutAsJsonAsync(goalsPath, new { goals = new[] { missingDirections } }), "goals[0].directionIds");
        await ProfileTestData.AssertFieldErrorAsync(
            await client.PutAsJsonAsync(goalsPath, new { goals = new[] { wrongDeadline } }), "goals[0].deadlineId");
        await ProfileTestData.AssertFieldErrorAsync(
            await client.PutAsJsonAsync(goalsPath, new { goals = new[] { original, original } }), "goals");

        var profile = await client.GetFromJsonAsync<MenteeProfileDto>(Path);
        Assert.Equal(original.Id, Assert.Single(profile!.Goals).Id);
    }

    [Fact]
    public async Task PutGoals_RequiresAuthenticationAndAnExistingMenteeProfile()
    {
        var goalsPath = $"{Path}/goals";
        var body = new { goals = Array.Empty<ProfileGoalDto>() };
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().PutAsJsonAsync(goalsPath, body)).StatusCode);

        var (client, _) = await factory.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(goalsPath, body)).StatusCode);
    }

    [Fact]
    public async Task PutAvailability_SavesAndReplacesScheduleIndependentlyOfQuestionnaire()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var input = await ValidProfileAsync();
        var questionnaire = await client.PutAsJsonAsync(Path, input);
        questionnaire.EnsureSuccessStatusCode();
        var starting = await questionnaire.ReadAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, starting.GetProperty("availability").ValueKind);

        var catalogs = await factory.CatalogsAsync();
        var city = catalogs[CatalogKinds.Cities].First(id => id != input.Personal.CityId);
        var both = ValidSchedule() with { Formats = ["offline", "online"], MeetingCityId = city };
        var path = $"{Path}/availability";
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(path, both)).StatusCode);

        var profile = await client.GetFromJsonAsync<JsonElement>(Path);
        var schedule = profile.GetProperty("availability").Deserialize<ScheduleDto>(JsonSerializerOptions.Web)!;
        Assert.Equal(new[] { "monday", "friday" }, schedule.Days);
        Assert.Equal(new[] { "online", "offline" }, schedule.Formats);
        Assert.Equal(city, schedule.MeetingCity!.Id);
        Assert.Equal("Asia/Yekaterinburg", schedule.TimeZoneId);
        Assert.Equal(("18:00", "21:00"), (schedule.StartTime, schedule.EndTime));
        Assert.Equal(input.Personal.CityId, profile.GetProperty("personal").GetProperty("city").GetProperty("id").GetGuid());
        Assert.Equal(input.AvailabilityId, profile.GetProperty("weeklyAvailability").GetProperty("id").GetGuid());
        Assert.Equal(input.FormatId, profile.GetProperty("format").GetProperty("id").GetGuid());
        Assert.Single(profile.GetProperty("goals").EnumerateArray());

        var updated = ValidSchedule() with { Days = ["tuesday"], TimeZoneId = "UTC", StartTime = "09:30", EndTime = "12:00" };
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(path, updated)).StatusCode);

        var newHours = catalogs[CatalogKinds.Availability].First(id => id != input.AvailabilityId);
        var repeated = await client.PutAsJsonAsync(Path, input with { AvailabilityId = newHours });
        repeated.EnsureSuccessStatusCode();
        var persisted = await repeated.ReadAsync<JsonElement>();
        var replaced = persisted.GetProperty("availability").Deserialize<ScheduleDto>(JsonSerializerOptions.Web)!;
        Assert.Equal(newHours, persisted.GetProperty("weeklyAvailability").GetProperty("id").GetGuid());
        Assert.Equal(updated.Days, replaced.Days);
        Assert.Equal(updated.Formats, replaced.Formats);
        Assert.Null(replaced.MeetingCity);
        Assert.Equal((updated.StartTime, updated.EndTime, updated.TimeZoneId), (replaced.StartTime, replaced.EndTime, replaced.TimeZoneId));
    }

    [Fact]
    public async Task PutAvailability_ValidatesScheduleAndMeetingCity()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var input = await ValidProfileAsync();
        (await client.PutAsJsonAsync(Path, input)).EnsureSuccessStatusCode();
        var valid = ValidSchedule();

        (ScheduleInput Input, string Field)[] invalid =
        [
            (valid with { Days = ["monday", "monday"] }, "days"),
            (valid with { Formats = ["online", "online"] }, "formats"),
            (valid with { Formats = ["hybrid"] }, "formats[0]"),
            (valid with { TimeZoneId = "Asia/Unknown" }, "timeZoneId"),
            (valid with { EndTime = "08:00" }, "endTime"),
            (valid with { StartTime = "25:00" }, "startTime"),
            (valid with { Formats = ["offline"] }, "meetingCityId"),
            (valid with { Formats = ["offline"], MeetingCityId = input.FormatId }, "meetingCityId"),
            (valid with { MeetingCityId = input.Personal.CityId }, "meetingCityId"),
        ];

        foreach (var (body, field) in invalid)
        {
            var response = await client.PutAsJsonAsync($"{Path}/availability", body);
            await ProfileTestData.AssertFieldErrorAsync(response, field);
        }

        var profile = await client.GetFromJsonAsync<JsonElement>(Path);
        Assert.Equal(JsonValueKind.Null, profile.GetProperty("availability").ValueKind);
    }

    [Fact]
    public async Task PutAvailability_RequiresAuthenticationAndAnExistingMenteeProfile()
    {
        var path = $"{Path}/availability";
        var body = ValidSchedule();
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().PutAsJsonAsync(path, body)).StatusCode);
        var (client, _) = await factory.SignedInClientAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(path, body)).StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsSelectedCatalogValues()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var template = await ValidProfileAsync();
        var python = await CatalogIdAsync(client, CatalogKinds.Skills, "Python");
        var input = template with
        {
            Personal = template.Personal with
            {
                CityId = await CatalogIdAsync(client, CatalogKinds.Cities, "Москва"),
                LanguageId = await CatalogIdAsync(client, CatalogKinds.Languages, "Русский"),
            },
            SkillIds = [python, await CatalogIdAsync(client, CatalogKinds.Skills, "SQL")],
            SkillLevelId = await CatalogIdAsync(client, CatalogKinds.SkillLevels, "Базовый"),
            TaskIds = [await CatalogIdAsync(client, CatalogKinds.MenteeTasks, "Освоить новый навык")],
            FormatId = await CatalogIdAsync(client, CatalogKinds.Formats, "Онлайн"),
            AvailabilityId = await CatalogIdAsync(client, CatalogKinds.Availability, "1–2 часа в неделю"),
            GoalDeadlineId = await CatalogIdAsync(client, CatalogKinds.GoalDeadlines, "1 месяц"),
        };
        var questionnaire = await client.PutAsJsonAsync(Path, input);
        questionnaire.EnsureSuccessStatusCode();
        var initial = await questionnaire.ReadAsync<JsonElement>();
        AssertLookup(initial.GetProperty("personal").GetProperty("language"), input.Personal.LanguageId, "Русский");
        Assert.Equal(JsonValueKind.Null, initial.GetProperty("availability").ValueKind);

        var cityId = await CatalogIdAsync(client, CatalogKinds.Cities, "Екатеринбург");
        var levelId = await CatalogIdAsync(client, CatalogKinds.SkillLevels, "Уверенный");
        var ownedSkill = new OwnedSkillDto(python, levelId);
        var directionId = await CatalogIdAsync(client, CatalogKinds.MentoringDirections, "Карьерный рост");
        var goal = new ProfileGoalDto(Guid.NewGuid(), "Подготовить проект для собеседования",
            await CatalogIdAsync(client, CatalogKinds.GoalDeadlines, "3 месяца"), [directionId]);
        var availability = ValidSchedule() with { Formats = ["offline"], MeetingCityId = cityId };
        (await client.PutAsJsonAsync($"{Path}/skills", new { ownedSkills = new[] { ownedSkill } })).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"{Path}/goals", new { goals = new[] { goal } })).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"{Path}/availability", availability)).EnsureSuccessStatusCode();

        var personalUpdate = await client.PutAsJsonAsync($"{Path}/personal", PersonalDetails(input));
        personalUpdate.EnsureSuccessStatusCode();
        var personal = await personalUpdate.ReadAsync<JsonElement>();
        AssertLookup(personal.GetProperty("city"), input.Personal.CityId, "Москва");
        AssertLookup(personal.GetProperty("language"), input.Personal.LanguageId, "Русский");

        var profile = await client.GetFromJsonAsync<JsonElement>(Path);
        AssertLookup(profile.GetProperty("personal").GetProperty("city"), input.Personal.CityId, "Москва");
        Assert.Equal(input.SkillIds.Order(),
            profile.GetProperty("skills").EnumerateArray().Select(skill => skill.GetProperty("id").GetGuid()));
        AssertLookup(profile.GetProperty("skillLevel"), input.SkillLevelId, "Базовый");
        AssertLookup(Assert.Single(profile.GetProperty("tasks").EnumerateArray()), input.TaskIds[0], "Освоить новый навык");
        AssertLookup(profile.GetProperty("format"), input.FormatId, "Онлайн");
        AssertLookup(profile.GetProperty("weeklyAvailability"), input.AvailabilityId, "1–2 часа в неделю");
        AssertLookup(profile.GetProperty("goalDeadline"), input.GoalDeadlineId, "1 месяц");

        var skill = Assert.Single(profile.GetProperty("ownedSkills").EnumerateArray());
        AssertLookup(skill.GetProperty("skill"), ownedSkill.SkillId, "Python");
        AssertLookup(skill.GetProperty("level"), levelId, "Уверенный");
        var savedGoal = Assert.Single(profile.GetProperty("goals").EnumerateArray());
        AssertLookup(savedGoal.GetProperty("deadline"), goal.DeadlineId, "3 месяца");
        AssertLookup(Assert.Single(savedGoal.GetProperty("directions").EnumerateArray()), directionId, "Карьерный рост");
        var schedule = profile.GetProperty("availability");
        AssertLookup(schedule.GetProperty("meetingCity"), cityId, "Екатеринбург");

        Assert.Equal(goal.Id, savedGoal.GetProperty("id").GetGuid());
    }

    private static async Task<Guid> CatalogIdAsync(HttpClient client, string kind, string name)
    {
        var page = await client.GetFromJsonAsync<JsonElement>($"/api/v1/catalogs/{kind}?q={Uri.EscapeDataString(name)}");
        var entry = Assert.Single(page.GetProperty("items").EnumerateArray(), item => item.GetProperty("name").GetString() == name);
        return entry.GetProperty("id").GetGuid();
    }

    private static void AssertLookup(JsonElement value, Guid expectedId, string expectedName)
    {
        Assert.Equal(expectedId, value.GetProperty("id").GetGuid());
        Assert.Equal(expectedName, value.GetProperty("name").GetString());
    }

    private sealed record ScheduleInput(string[] Days, string StartTime, string EndTime,
        string TimeZoneId, string[] Formats, Guid? MeetingCityId);

    private sealed record ScheduleDto(string[] Days, string StartTime, string EndTime,
        string TimeZoneId, string[] Formats, ProfileLookupDto? MeetingCity);

    private static ScheduleInput ValidSchedule() =>
        new(["friday", "monday"], "18:00", "21:00", "Asia/Yekaterinburg", ["online"], null);

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
