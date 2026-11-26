using System.Net;
using System.Net.Http.Json;
using Domain.Catalogs;
using Domain.Users;
using IntegrationTests.Infrastructure;
using IntegrationTests.Mentees;

namespace IntegrationTests.Mentors;

public sealed record MentorSkillInput(Guid SkillId, Guid SkillLevelId);

public sealed record MentorProfileInput(
    PersonalInput Personal,
    Guid PositionId,
    Guid ExperienceId,
    MentorSkillInput[] Skills,
    Guid[] DirectionIds,
    Guid FormatId,
    Guid AvailabilityId,
    Guid PricingModelId,
    string Price,
    bool HasFreeTrial);

public sealed record MentorWorkplaceDto(Guid Id, string Company, string Title, int StartYear, int? EndYear, bool IsCurrent);

public sealed record MentorWorkExperienceDto(MentorWorkplaceDto[] Workplaces);

public sealed record MentorProfileDto(
    Guid UserId,
    PersonalDto Personal,
    Guid PositionId,
    Guid ExperienceId,
    MentorSkillInput[] Skills,
    Guid[] DirectionIds,
    Guid FormatId,
    Guid AvailabilityId,
    Guid PricingModelId,
    string Price,
    bool HasFreeTrial,
    MentorWorkplaceDto[] Workplaces,
    DateTimeOffset UpdatedAt);

public class MentorProfileTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Path = "/api/v1/me/mentor-profile";

    [Fact]
    public async Task Get_BeforeSave_ReturnsNotFound()
    {
        var (client, _) = await factory.SignedInClientAsync();

        var response = await client.GetAsync(Path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_FirstSave_ReturnsProfileAndAssignsMentorRole()
    {
        var (client, tokens) = await factory.SignedInClientAsync();
        var input = await ValidProfileAsync();

        var response = await client.PutAsJsonAsync(Path, input);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.ReadAsync<MentorProfileDto>();
        Assert.Equal(tokens.Account.Id, saved.UserId);
        Assert.Equal(input.Skills.OrderBy(skill => skill.SkillId), saved.Skills);
        Assert.Equal(input.DirectionIds.Order(), saved.DirectionIds);
        Assert.Equal(input.PositionId, saved.PositionId);
        Assert.Equal(input.PricingModelId, saved.PricingModelId);
        Assert.Equal("1500.00", saved.Price);
        Assert.True(saved.HasFreeTrial);

        var refreshed = await client.RefreshAsync(tokens.RefreshToken);
        Assert.Equal(["mentor"], (await refreshed.ReadAsync<TokenPairDto>()).Account.Roles);

        var fetched = await (await client.GetAsync(Path)).ReadAsync<MentorProfileDto>();
        Assert.Equal(saved.Skills, fetched.Skills);
    }

    [Fact]
    public async Task Put_Again_ChangesSkillLevelsAndReplacesCollections()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var catalogs = await factory.CatalogsAsync();
        var first = await ValidProfileAsync();
        (await client.PutAsJsonAsync(Path, first)).EnsureSuccessStatusCode();

        var expert = catalogs[CatalogKinds.SkillLevels][3];
        var second = first with
        {
            Skills = [first.Skills[1] with { SkillLevelId = expert }, new(catalogs[CatalogKinds.Skills][5], expert)],
            DirectionIds = [catalogs[CatalogKinds.MentoringDirections][2]],
            Price = "0.00",
            HasFreeTrial = false,
        };

        (await client.PutAsJsonAsync(Path, second)).EnsureSuccessStatusCode();

        var fetched = await (await client.GetAsync(Path)).ReadAsync<MentorProfileDto>();
        Assert.Equal(second.Skills.OrderBy(skill => skill.SkillId), fetched.Skills);
        Assert.Equal(second.DirectionIds, fetched.DirectionIds);
        Assert.Equal("0.00", fetched.Price);
        Assert.False(fetched.HasFreeTrial);
    }

    [Fact]
    public async Task Put_AfterMenteeQuestionnaire_ReturnsRoleConflict()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var catalogs = await factory.CatalogsAsync();
        var mentee = new MenteeProfileInput(
            ProfileTestData.Personal(catalogs),
            [catalogs[CatalogKinds.Skills][0]],
            catalogs[CatalogKinds.SkillLevels][0],
            [catalogs[CatalogKinds.MenteeTasks][0]],
            catalogs[CatalogKinds.Formats][0],
            catalogs[CatalogKinds.Availability][0],
            null,
            null,
            "Освоить ASP.NET Core и собрать учебный API",
            catalogs[CatalogKinds.GoalDeadlines][0]);
        (await client.PutAsJsonAsync("/api/v1/me/mentee-profile", mentee)).EnsureSuccessStatusCode();

        var response = await client.PutAsJsonAsync(Path, await ValidProfileAsync());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("role_conflict", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task MenteePut_AfterMentorQuestionnaire_ReturnsRoleConflict()
    {
        var (client, _) = await factory.SignedInClientAsync();
        (await client.PutAsJsonAsync(Path, await ValidProfileAsync())).EnsureSuccessStatusCode();
        var catalogs = await factory.CatalogsAsync();

        var response = await client.PutAsJsonAsync("/api/v1/me/mentee-profile", new MenteeProfileInput(
            ProfileTestData.Personal(catalogs),
            [catalogs[CatalogKinds.Skills][0]],
            catalogs[CatalogKinds.SkillLevels][0],
            [catalogs[CatalogKinds.MenteeTasks][0]],
            catalogs[CatalogKinds.Formats][0],
            catalogs[CatalogKinds.Availability][0],
            null,
            null,
            "Освоить ASP.NET Core и собрать учебный API",
            catalogs[CatalogKinds.GoalDeadlines][0]));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Put_ForCurator_ReturnsForbidden()
    {
        var (client, tokens) = await factory.SignedInClientAsync();
        await factory.AddRoleAsync(tokens.Account.Id, Roles.Curator);

        var response = await client.PutAsJsonAsync(Path, await ValidProfileAsync());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("skillId")]
    [InlineData("skillLevelId")]
    [InlineData("duplicate")]
    public async Task Put_InvalidSkills_ReturnsSkillsError(string problem)
    {
        var (client, _) = await factory.SignedInClientAsync();
        var valid = await ValidProfileAsync();
        var skill = valid.Skills[0];

        MentorSkillInput[] skills = problem switch
        {
            "skillId" => [skill with { SkillId = valid.FormatId }],
            "skillLevelId" => [skill with { SkillLevelId = valid.FormatId }],
            _ => [skill, skill],
        };

        var response = await client.PutAsJsonAsync(Path, valid with { Skills = skills });

        await ProfileTestData.AssertFieldErrorAsync(response, "skills");
    }

    [Theory]
    [InlineData("positionId")]
    [InlineData("pricingModelId")]
    [InlineData("directionIds")]
    public async Task Put_IdFromOtherCatalog_ReturnsFieldError(string field)
    {
        var (client, _) = await factory.SignedInClientAsync();
        var valid = await ValidProfileAsync();
        var wrongId = valid.ExperienceId;

        var input = field switch
        {
            "positionId" => valid with { PositionId = wrongId },
            "pricingModelId" => valid with { PricingModelId = wrongId },
            _ => valid with { DirectionIds = [wrongId] },
        };

        var response = await client.PutAsJsonAsync(Path, input);

        await ProfileTestData.AssertFieldErrorAsync(response, field);
    }

    [Theory]
    [InlineData("1500")]
    [InlineData("-1.00")]
    [InlineData("10.555")]
    public async Task Put_InvalidPrice_ReturnsFieldError(string price)
    {
        var (client, _) = await factory.SignedInClientAsync();

        var response = await client.PutAsJsonAsync(Path, await ValidProfileAsync() with { Price = price });

        await ProfileTestData.AssertFieldErrorAsync(response, "price");
    }

    [Fact]
    public async Task PutWorkExperience_AssignsIdsAndReusesThemWhenTheListIsResent()
    {
        var (client, _) = await factory.SignedInClientAsync();
        var input = await ValidProfileAsync();
        var questionnaire = await client.PutAsJsonAsync(Path, input);
        questionnaire.EnsureSuccessStatusCode();
        Assert.Empty((await questionnaire.ReadAsync<MentorProfileDto>()).Workplaces);

        var current = new { Company = "Команда Б", Title = "Старший инженер", StartYear = 2022, EndYear = (int?)null, IsCurrent = true };
        var past = new { Company = "Команда А", Title = "Инженер", StartYear = 2018, EndYear = (int?)2022, IsCurrent = false };
        var path = $"{Path}/work-experience";
        var initialList = new { workplaces = new[] { current, past } };

        var saved = await client.PutAsJsonAsync(path, initialList);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var workplaces = (await saved.ReadAsync<MentorWorkExperienceDto>()).Workplaces;
        Assert.Equal(new[] { current.Company, past.Company }, workplaces.Select(workplace => workplace.Company));
        Assert.All(workplaces, workplace => Assert.NotEqual(Guid.Empty, workplace.Id));
        Assert.NotEqual(workplaces[0].Id, workplaces[1].Id);

        var retryWithoutIds = await client.PutAsJsonAsync(path, initialList);
        retryWithoutIds.EnsureSuccessStatusCode();
        Assert.Equal(workplaces, (await retryWithoutIds.ReadAsync<MentorWorkExperienceDto>()).Workplaces);

        var savedById = workplaces[0] with { Title = "Ведущий инженер" };
        var mixed = await client.PutAsJsonAsync(path, new { workplaces = new object[] { past, savedById } });
        mixed.EnsureSuccessStatusCode();
        Assert.Equal(new[] { workplaces[1], savedById }, (await mixed.ReadAsync<MentorWorkExperienceDto>()).Workplaces);

        var profile = await client.GetFromJsonAsync<MentorProfileDto>(Path);
        Assert.Equal(new[] { workplaces[1], savedById }, profile!.Workplaces);
        Assert.Equal(input.PositionId, profile.PositionId);
        Assert.Equal(input.ExperienceId, profile.ExperienceId);
        Assert.Equal(input.Skills.OrderBy(skill => skill.SkillId), profile.Skills);

        var replacement = await client.PutAsJsonAsync(path, new { workplaces = new[] { savedById } });
        replacement.EnsureSuccessStatusCode();
        Assert.Equal(savedById, Assert.Single((await replacement.ReadAsync<MentorWorkExperienceDto>()).Workplaces));

        var repeated = await client.PutAsJsonAsync(Path, input with { Price = "1200.00" });
        repeated.EnsureSuccessStatusCode();
        var after = await repeated.ReadAsync<MentorProfileDto>();
        Assert.Equal(savedById, Assert.Single(after.Workplaces));
        Assert.Equal("1200.00", after.Price);

        var cleared = await client.PutAsJsonAsync(path, new { workplaces = Array.Empty<MentorWorkplaceDto>() });
        cleared.EnsureSuccessStatusCode();
        Assert.Empty((await cleared.ReadAsync<MentorWorkExperienceDto>()).Workplaces);
        Assert.Empty((await client.GetFromJsonAsync<MentorProfileDto>(Path))!.Workplaces);
    }

    [Fact]
    public async Task PutWorkExperience_InvalidYearsAndDuplicateIdsPreserveSavedHistory()
    {
        var (client, _) = await factory.SignedInClientAsync();
        (await client.PutAsJsonAsync(Path, await ValidProfileAsync())).EnsureSuccessStatusCode();
        var path = $"{Path}/work-experience";
        var draft = new { company = "Команда", title = "Инженер", startYear = 2021, isCurrent = true };
        var saved = await client.PutAsJsonAsync(path, new { workplaces = new[] { draft } });
        saved.EnsureSuccessStatusCode();
        var workplace = Assert.Single((await saved.ReadAsync<MentorWorkExperienceDto>()).Workplaces);

        (MentorWorkplaceDto[] Choices, string Field)[] invalid =
        [
            ([workplace, workplace], "workplaces"),
            ([workplace with { Id = Guid.NewGuid() }], "workplaces[0].id"),
            ([workplace with { StartYear = 0 }], "workplaces[0].startYear"),
            ([workplace with { StartYear = 9999 }], "workplaces[0].startYear"),
            ([workplace with { EndYear = 2022 }], "workplaces[0].endYear"),
            ([workplace with { IsCurrent = false, EndYear = 2020 }], "workplaces[0].endYear"),
        ];

        foreach (var (choices, field) in invalid)
        {
            var response = await client.PutAsJsonAsync(path, new { workplaces = choices });
            await ProfileTestData.AssertFieldErrorAsync(response, field);
        }

        var profile = await client.GetFromJsonAsync<MentorProfileDto>(Path);
        Assert.Equal(workplace, Assert.Single(profile!.Workplaces));
    }

    [Fact]
    public async Task PutWorkExperience_RequiresAuthenticationAndAnExistingMentorProfile()
    {
        var path = $"{Path}/work-experience";
        var body = new { workplaces = Array.Empty<MentorWorkplaceDto>() };
        var anonymous = await factory.CreateClient().PutAsJsonAsync(path, body);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var (client, _) = await factory.SignedInClientAsync();
        var missing = await client.PutAsJsonAsync(path, body);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("resource_not_found", (await missing.ReadAsync<ProblemDto>()).Code);
    }

    private async Task<MentorProfileInput> ValidProfileAsync()
    {
        var catalogs = await factory.CatalogsAsync();
        var levels = catalogs[CatalogKinds.SkillLevels];

        return new MentorProfileInput(
            ProfileTestData.Personal(catalogs) with { FirstName = "Иван", LastName = "Петров", Age = 34 },
            catalogs[CatalogKinds.Positions][0],
            catalogs[CatalogKinds.Experience][2],
            [new(catalogs[CatalogKinds.Skills][0], levels[2]), new(catalogs[CatalogKinds.Skills][1], levels[1])],
            [catalogs[CatalogKinds.MentoringDirections][0], catalogs[CatalogKinds.MentoringDirections][1]],
            catalogs[CatalogKinds.Formats][0],
            catalogs[CatalogKinds.Availability][1],
            catalogs[CatalogKinds.PricingModels][0],
            "1500.00",
            HasFreeTrial: true);
    }
}
