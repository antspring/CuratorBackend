using System.Net;
using System.Net.Http.Json;
using Domain.Catalogs;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Mentors;

public sealed record LookupDto(Guid Id, string Name);

public sealed record MentorSkillViewDto(LookupDto Skill, LookupDto Level);

public sealed record MentorCurrentWorkplaceDto(string Company, string Title);

public sealed record MentorCardDto(
    Guid UserId,
    string FirstName,
    string LastName,
    int Age,
    string? PhotoUrl,
    LookupDto City,
    LookupDto Language,
    LookupDto Position,
    MentorCurrentWorkplaceDto? CurrentWorkplace,
    LookupDto Experience,
    MentorSkillViewDto[] Skills,
    LookupDto[] Directions,
    LookupDto Format,
    LookupDto Availability,
    LookupDto PricingModel,
    string Price,
    bool HasFreeTrial,
    decimal? Rating,
    int ReviewCount,
    int CompletedMentorshipCount,
    DateTimeOffset? NextAvailableAt);

public class MentorCardTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Get_ReturnsCardWithCatalogNamesToAnyUser()
    {
        var (mentorClient, mentorTokens) = await factory.SignedInClientAsync();
        var catalogs = await factory.CatalogsAsync();
        var photoId = await ProfileTestData.UploadAvatarAsync(mentorClient);
        var input = ProfileInput(catalogs, photoId);
        (await mentorClient.PutAsJsonAsync("/api/v1/me/mentor-profile", input)).EnsureSuccessStatusCode();
        var (viewer, _) = await factory.SignedInClientAsync();

        var response = await viewer.GetAsync($"/api/v1/mentors/{mentorTokens.Account.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var card = await response.ReadAsync<MentorCardDto>();
        Assert.Equal(mentorTokens.Account.Id, card.UserId);
        Assert.Equal(("Иван", "Петров", 34), (card.FirstName, card.LastName, card.Age));
        Assert.StartsWith($"{factory.StorageUrl}/{ApiFactory.StorageBucket}/avatars/", card.PhotoUrl);
        using var browser = new HttpClient();
        using var avatar = await browser.GetAsync(card.PhotoUrl);
        Assert.Equal(HttpStatusCode.OK, avatar.StatusCode);
        Assert.Equal("image/png", avatar.Content.Headers.ContentType?.MediaType);
        Assert.Equal(input.Personal.CityId, card.City.Id);
        Assert.False(string.IsNullOrEmpty(card.City.Name));
        Assert.Equal(input.PositionId, card.Position.Id);
        Assert.False(string.IsNullOrWhiteSpace(card.Position.Name));
        Assert.Null(card.CurrentWorkplace);
        Assert.Equal(input.PricingModelId, card.PricingModel.Id);
        Assert.Equal("1500.00", card.Price);
        Assert.True(card.HasFreeTrial);
        Assert.Equal(
            input.OwnedSkills.Select(skill => (skill.SkillId, skill.SkillLevelId)).Order(),
            card.Skills.Select(skill => (skill.Skill.Id, skill.Level.Id)).Order());
        Assert.Equal("Эксперт", card.Skills.Single(skill => skill.Skill.Id == input.OwnedSkills[0].SkillId).Level.Name);
        Assert.Equal([input.DirectionIds[1], input.DirectionIds[0]], card.Directions.Select(direction => direction.Id));
        Assert.Null(card.Rating);
        Assert.Equal(0, card.ReviewCount);
        Assert.Equal(0, card.CompletedMentorshipCount);
        Assert.Null(card.NextAvailableAt);

        var withoutPhoto = input with { Personal = input.Personal with { PhotoFileId = null } };
        (await mentorClient.PutAsJsonAsync("/api/v1/me/mentor-profile", withoutPhoto)).EnsureSuccessStatusCode();
        var updated = await viewer.GetFromJsonAsync<MentorCardDto>($"/api/v1/mentors/{mentorTokens.Account.Id}");
        Assert.NotNull(updated);
        Assert.Null(updated.PhotoUrl);
    }

    [Fact]
    public async Task Get_ShowsFirstCurrentWorkplaceInProfileOrder()
    {
        var (mentor, tokens) = await factory.SignedInClientAsync();
        var (viewer, _) = await factory.SignedInClientAsync();
        var input = ProfileInput(await factory.CatalogsAsync());
        (await mentor.PutAsJsonAsync("/api/v1/me/mentor-profile", input)).EnsureSuccessStatusCode();
        var former = new { company = "Предыдущая компания", title = "Инженер", startYear = 2017, endYear = (int?)2020, isCurrent = false };
        var first = new { company = "Первая команда", title = "Руководитель разработки", startYear = 2020, endYear = (int?)null, isCurrent = true };
        var second = new { company = "Вторая команда", title = "Архитектор", startYear = 2023, endYear = (int?)null, isCurrent = true };
        var cardPath = $"/api/v1/mentors/{tokens.Account.Id}";
        (await mentor.PatchAsJsonAsync("/api/v1/me/mentor-profile", new { workplaces = new[] { former, first, second } })).EnsureSuccessStatusCode();
        var card = await viewer.GetFromJsonAsync<MentorCardDto>(cardPath);
        Assert.Equal(new MentorCurrentWorkplaceDto(first.company, first.title), card!.CurrentWorkplace);
        Assert.Equal(input.PositionId, card.Position.Id);

        (await mentor.PatchAsJsonAsync("/api/v1/me/mentor-profile", new { workplaces = new[] { former, second, first } })).EnsureSuccessStatusCode();
        var reordered = await viewer.GetFromJsonAsync<MentorCardDto>(cardPath);
        Assert.Equal(new MentorCurrentWorkplaceDto(second.company, second.title), reordered!.CurrentWorkplace);

        (await mentor.PatchAsJsonAsync("/api/v1/me/mentor-profile", new { workplaces = new[] { former } })).EnsureSuccessStatusCode();
        var withoutCurrent = await viewer.GetFromJsonAsync<MentorCardDto>(cardPath);
        Assert.Null(withoutCurrent!.CurrentWorkplace);
        Assert.Equal(card.Position, withoutCurrent.Position);
    }

    private static MentorProfileInput ProfileInput(Dictionary<string, List<Guid>> catalogs, Guid? photoId = null)
    {
        var levels = catalogs[CatalogKinds.SkillLevels];
        return new MentorProfileInput(
            ProfileTestData.Personal(catalogs) with { FirstName = "Иван", LastName = "Петров", Age = 34, PhotoFileId = photoId },
            catalogs[CatalogKinds.Positions][0],
            catalogs[CatalogKinds.Experience][2],
            [new(catalogs[CatalogKinds.Skills][0], levels[3]), new(catalogs[CatalogKinds.Skills][1], levels[0])],
            [catalogs[CatalogKinds.MentoringDirections][3], catalogs[CatalogKinds.MentoringDirections][0]],
            catalogs[CatalogKinds.Formats][0],
            catalogs[CatalogKinds.Availability][1],
            catalogs[CatalogKinds.PricingModels][0],
            "1500.00",
            HasFreeTrial: true);
    }

    [Fact]
    public async Task Get_UserWithoutMentorProfile_ReturnsNotFound()
    {
        var (client, tokens) = await factory.SignedInClientAsync();

        var own = await client.GetAsync($"/api/v1/mentors/{tokens.Account.Id}");
        var unknown = await client.GetAsync($"/api/v1/mentors/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, own.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("resource_not_found", (await unknown.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Get_WithoutToken_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync($"/api/v1/mentors/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
