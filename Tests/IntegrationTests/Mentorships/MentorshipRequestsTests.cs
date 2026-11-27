using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Domain.Catalogs;
using Domain.Users;
using Infrastructure.Persistence;
using IntegrationTests.Infrastructure;
using IntegrationTests.Mentees;
using IntegrationTests.Mentors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Mentorships;

public sealed class MentorshipRequestsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string RequestsPath = "/api/v1/mentorship-requests";
    private const string MentorshipsPath = "/api/v1/mentorships";

    [Fact]
    public async Task Accept_ConcurrentCallsCreateOnePairAndOnlyParticipantsCanReadIt()
    {
        var (mentee, menteeId) = await ParticipantAsync(Roles.Mentee);
        var (mentor, mentorId) = await ParticipantAsync(Roles.Mentor);
        var (otherMentor, _) = await ParticipantAsync(Roles.Mentor);

        var sends = await Task.WhenAll(
            mentee.PostAsJsonAsync(RequestsPath, new { mentorId, durationMonths = 3 }),
            mentee.PostAsJsonAsync(RequestsPath, new { mentorId, durationMonths = 3 }));
        var created = Assert.Single(sends, response => response.StatusCode == HttpStatusCode.Created);
        var duplicate = Assert.Single(sends, response => response.StatusCode == HttpStatusCode.Conflict);
        await AssertProblemAsync(duplicate, HttpStatusCode.Conflict, "request_exists");
        var request = await created.ReadAsync<RequestDto>();
        Assert.Equal((menteeId, mentorId, 3, "pending"), (request.MenteeId, request.MentorId, request.DurationMonths, request.Status));
        Assert.Null(request.MentorshipId);

        var location = created.Headers.Location;
        Assert.NotNull(location);
        Assert.Equal($"{RequestsPath}/{request.Id}", location.ToString());
        Assert.Equal(request.Id, (await mentor.GetFromJsonAsync<RequestDto>(location))!.Id);
        await AssertProblemAsync(await otherMentor.GetAsync(location), HttpStatusCode.NotFound, "resource_not_found");

        var acceptPath = $"{location}/accept";
        await AssertProblemAsync(await mentee.PostAsync(acceptPath, null), HttpStatusCode.Forbidden, "forbidden");
        await AssertProblemAsync(await otherMentor.PostAsync(acceptPath, null), HttpStatusCode.NotFound, "resource_not_found");

        var accepts = await Task.WhenAll(mentor.PostAsync(acceptPath, null), mentor.PostAsync(acceptPath, null));
        Assert.All(accepts, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var accepted = await accepts[0].ReadAsync<AcceptanceDto>();
        var repeated = await accepts[1].ReadAsync<AcceptanceDto>();
        Assert.Equal(accepted, repeated);
        Assert.Equal("accepted", accepted.Request.Status);
        Assert.Equal(accepted.Mentorship.Id, accepted.Request.MentorshipId);
        Assert.Equal(request.Id, accepted.Mentorship.RequestId);
        Assert.Equal((menteeId, mentorId, "active"),
            (accepted.Mentorship.MenteeId, accepted.Mentorship.MentorId, accepted.Mentorship.Status));
        Assert.Equal(accepted.Mentorship.StartsAt, accepted.Request.DecidedAt);
        Assert.Equal(accepted.Mentorship.StartsAt.AddMonths(3), accepted.Mentorship.PlannedEndsAt);

        var pairPath = $"{MentorshipsPath}/{accepted.Mentorship.Id}";
        Assert.Equal(accepted.Mentorship.Id, (await mentee.GetFromJsonAsync<MentorshipDto>(pairPath))!.Id);
        Assert.Equal(accepted.Mentorship.Id, (await mentor.GetFromJsonAsync<MentorshipDto>(pairPath))!.Id);
        await AssertProblemAsync(await otherMentor.GetAsync(pairPath), HttpStatusCode.NotFound, "resource_not_found");
        var activeConflict = await mentee.PostAsJsonAsync(RequestsPath, new { mentorId, durationMonths = 6 });
        await AssertProblemAsync(activeConflict, HttpStatusCode.Conflict, "active_mentorship_exists");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Mentorships.CountAsync(mentorship => mentorship.RequestId == request.Id));
    }

    [Fact]
    public async Task List_ScopesRequestsAndBindsCursorToFilters()
    {
        var (mentee, _) = await ParticipantAsync(Roles.Mentee);
        var (mentor, mentorId) = await ParticipantAsync(Roles.Mentor);
        var (_, otherMentorId) = await ParticipantAsync(Roles.Mentor);
        var incomingRequest = await mentee.PostAsJsonAsync(RequestsPath, new { mentorId, durationMonths = 1 });
        incomingRequest.EnsureSuccessStatusCode();
        var expected = await incomingRequest.ReadAsync<RequestDto>();
        var sentToAnother = await mentee.PostAsJsonAsync(RequestsPath, new { mentorId = otherMentorId, durationMonths = 6 });
        sentToAnother.EnsureSuccessStatusCode();

        var incoming = await mentor.GetFromJsonAsync<RequestPageDto>($"{RequestsPath}?direction=incoming&status=pending");
        Assert.Equal(expected.Id, Assert.Single(incoming!.Items).Id);
        Assert.Null(incoming.NextCursor);
        var firstPage = await mentee.GetFromJsonAsync<RequestPageDto>($"{RequestsPath}?limit=1&status=pending");
        var newest = Assert.Single(firstPage!.Items);
        Assert.Equal(otherMentorId, newest.MentorId);
        var cursor = Uri.EscapeDataString(Assert.IsType<string>(firstPage.NextCursor));
        var secondPage = await mentee.GetFromJsonAsync<RequestPageDto>($"{RequestsPath}?limit=1&status=pending&cursor={cursor}");
        Assert.Equal(expected.Id, Assert.Single(secondPage!.Items).Id);
        Assert.Null(secondPage.NextCursor);

        await AssertProblemAsync(await mentee.GetAsync($"{RequestsPath}?status=accepted&cursor={cursor}"),
            HttpStatusCode.BadRequest, "invalid_cursor");
    }

    [Fact]
    public async Task Send_RequiresProfilesAndValidRequestData()
    {
        var (mentee, menteeId) = await ParticipantAsync(Roles.Mentee);
        var (mentor, mentorId) = await ParticipantAsync(Roles.Mentor);
        var (newUser, _) = await factory.SignedInClientAsync();
        var input = new { mentorId, durationMonths = 1 };

        await AssertProblemAsync(await factory.CreateClient().PostAsJsonAsync(RequestsPath, input),
            HttpStatusCode.Unauthorized, "unauthenticated");
        await AssertProblemAsync(await newUser.PostAsJsonAsync(RequestsPath, input),
            HttpStatusCode.Conflict, "profile_incomplete");
        await AssertProblemAsync(await mentor.PostAsJsonAsync(RequestsPath, input), HttpStatusCode.Forbidden, "forbidden");
        await AssertProblemAsync(await mentee.PostAsJsonAsync(RequestsPath, new { mentorId = Guid.NewGuid(), durationMonths = 1 }),
            HttpStatusCode.NotFound, "resource_not_found");
        await ProfileTestData.AssertFieldErrorAsync(
            await mentee.PostAsJsonAsync(RequestsPath, new { mentorId, durationMonths = 2 }), "durationMonths");
        await ProfileTestData.AssertFieldErrorAsync(
            await mentee.PostAsJsonAsync(RequestsPath, new { mentorId = menteeId, durationMonths = 1 }), "mentorId");
    }

    private async Task<(HttpClient Client, Guid UserId)> ParticipantAsync(string role)
    {
        var (client, tokens) = await factory.SignedInClientAsync();
        var catalogs = await factory.CatalogsAsync();
        var personal = ProfileTestData.Personal(catalogs);
        var skillId = catalogs[CatalogKinds.Skills][0];
        var levelId = catalogs[CatalogKinds.SkillLevels][0];
        var formatId = catalogs[CatalogKinds.Formats][0];
        var availabilityId = catalogs[CatalogKinds.Availability][0];
        if (role == Roles.Mentor)
        {
            var input = new MentorProfileInput(personal, catalogs[CatalogKinds.Positions][0], catalogs[CatalogKinds.Experience][0],
                [new(skillId, levelId)], [catalogs[CatalogKinds.MentoringDirections][0]], formatId, availabilityId,
                catalogs[CatalogKinds.PricingModels][0], "1500.00", true);
            (await client.PutAsJsonAsync("/api/v1/me/mentor-profile", input)).EnsureSuccessStatusCode();
        }
        else
        {
            var input = new MenteeProfileInput(personal, [skillId], [new(skillId, levelId)], [catalogs[CatalogKinds.MenteeTasks][0]],
                formatId, availabilityId, null, null,
                [new(null, "Освоить ASP.NET Core и собрать учебный API", catalogs[CatalogKinds.GoalDeadlines][0], [])]);
            (await client.PutAsJsonAsync("/api/v1/me/mentee-profile", input)).EnsureSuccessStatusCode();
        }

        var refreshed = await client.RefreshAsync(tokens.RefreshToken);
        refreshed.EnsureSuccessStatusCode();
        var current = await refreshed.ReadAsync<TokenPairDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", current.AccessToken);
        return (client, current.Account.Id);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await response.ReadAsync<ProblemDto>()).Code);
    }

    private sealed record RequestDto(Guid Id, Guid MenteeId, Guid MentorId, int DurationMonths, string Status,
        DateTimeOffset? DecidedAt, Guid? MentorshipId);

    private sealed record MentorshipDto(Guid Id, Guid RequestId, Guid MenteeId, Guid MentorId, string Status,
        DateTimeOffset StartsAt, DateTimeOffset PlannedEndsAt);

    private sealed record RequestPageDto(RequestDto[] Items, string? NextCursor);

    private sealed record AcceptanceDto(RequestDto Request, MentorshipDto Mentorship);
}
