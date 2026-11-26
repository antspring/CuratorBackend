using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Auth;

public class RegistrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Register_RequiresMailedCodeBeforeLogin_AndConsumesItOnce()
    {
        var email = ApiClient.NewEmail();

        var started = await client.RegisterAsync(email);
        Assert.Equal(HttpStatusCode.Accepted, started.StatusCode);
        var ticket = await started.ReadAsync<TicketDto>();
        Assert.Equal(TimeSpan.FromMinutes(9.5), ticket.ExpiresAt - ticket.ResendAvailableAt);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.LoginAsync(email)).StatusCode);
        var code = await factory.ReadCodeAsync(email);
        var response = await client.ConfirmAsync(ticket.VerificationId, code);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/me", response.Headers.Location?.OriginalString);

        var account = await response.ReadAsync<AccountDto>();
        Assert.Equal(email, account.Email);
        Assert.Empty(account.Roles);
        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal(HttpStatusCode.OK, (await client.LoginAsync(email)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.ConfirmAsync(ticket.VerificationId, code)).StatusCode);
    }

    [Fact]
    public async Task Register_SameEmailInOtherCase_ReturnsAccountExists()
    {
        var email = ApiClient.NewEmail();
        (await client.RegisterConfirmedAsync(factory, email)).EnsureSuccessStatusCode();

        var response = await client.RegisterAsync(email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("account_exists", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Resend_EnforcesCooldownThenReplacesTheCode()
    {
        var email = ApiClient.NewEmail();
        var ticket = await (await client.RegisterAsync(email)).ReadAsync<TicketDto>();
        var firstCode = await factory.ReadCodeAsync(email);
        var limited = await client.ResendAsync(ticket.VerificationId);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.NotNull(limited.Headers.RetryAfter);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.RegisterAsync(email.ToUpperInvariant())).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE pending_registrations SET resend_available_at = now() WHERE verification_id = {ticket.VerificationId}");
        Assert.Equal(HttpStatusCode.Accepted, (await client.ResendAsync(ticket.VerificationId)).StatusCode);
        var newCode = await factory.ReadCodeAsync(email, expectedMessages: 2);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.ConfirmAsync(ticket.VerificationId, firstCode)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.ConfirmAsync(ticket.VerificationId, newCode)).StatusCode);
    }

    [Fact]
    public async Task Confirm_FiveWrongCodesExhaustAttemptsEvenWhenSubmittedTogether()
    {
        var email = ApiClient.NewEmail();
        var ticket = await (await client.RegisterAsync(email)).ReadAsync<TicketDto>();
        var code = await factory.ReadCodeAsync(email);
        var wrong = code == "000000" ? "000001" : "000000";
        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => client.ConfirmAsync(ticket.VerificationId, wrong)));
        Assert.Equal(4, responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Gone);
        Assert.Equal(HttpStatusCode.Gone, (await client.ConfirmAsync(ticket.VerificationId, code)).StatusCode);
    }

    [Fact]
    public async Task Confirm_ExpiredCodeRequiresNewEmail()
    {
        var email = ApiClient.NewEmail();
        var ticket = await (await client.RegisterAsync(email)).ReadAsync<TicketDto>();
        var code = await factory.ReadCodeAsync(email);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE pending_registrations SET expires_at = now(), resend_available_at = now() WHERE verification_id = {ticket.VerificationId}");
        Assert.Equal(HttpStatusCode.Gone, (await client.ConfirmAsync(ticket.VerificationId, code)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.LoginAsync(email)).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await client.ResendAsync(ticket.VerificationId)).StatusCode);
        var renewed = await factory.ReadCodeAsync(email, expectedMessages: 2);
        Assert.Equal(HttpStatusCode.Created, (await client.ConfirmAsync(ticket.VerificationId, renewed)).StatusCode);
    }

    [Fact]
    public async Task RestartingRegistrationBindsTheNewPasswordToANewTicket()
    {
        var email = ApiClient.NewEmail();
        var starts = await Task.WhenAll(client.RegisterAsync(email), client.RegisterAsync(email));
        var first = await Assert.Single(starts, response => response.StatusCode == HttpStatusCode.Accepted).ReadAsync<TicketDto>();
        Assert.Single(starts, response => response.StatusCode == HttpStatusCode.TooManyRequests);
        var firstCode = await factory.ReadCodeAsync(email);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE pending_registrations SET resend_available_at = now() WHERE verification_id = {first.VerificationId}");
        const string password = "my replacement password";
        var restart = await client.PostAsJsonAsync("/api/v1/accounts", new { email, password });
        Assert.Equal(HttpStatusCode.Accepted, restart.StatusCode);
        var second = await restart.ReadAsync<TicketDto>();
        Assert.NotEqual(first.VerificationId, second.VerificationId);
        Assert.Equal(HttpStatusCode.Gone, (await client.ConfirmAsync(first.VerificationId, firstCode)).StatusCode);
        var secondCode = await factory.ReadCodeAsync(email, expectedMessages: 2);
        var confirms = await Task.WhenAll(client.ConfirmAsync(second.VerificationId, secondCode), client.ConfirmAsync(second.VerificationId, secondCode));
        Assert.Single(confirms, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(confirms, response => response.StatusCode == HttpStatusCode.Gone);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.LoginAsync(email)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.LoginAsync(email, password)).StatusCode);
    }

    [Fact]
    public async Task Register_ShortPasswordAndMissingEmail_ReturnsFieldErrors()
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new { password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Contains("password", problem.Errors!.Keys);
        Assert.Contains("email", problem.Errors.Keys);
    }

    [Theory]
    [InlineData("roles")]
    [InlineData("isAdmin")]
    public async Task Register_UnknownJsonField_ReturnsValidationError(string field)
    {
        var body = new Dictionary<string, object>
        {
            ["email"] = ApiClient.NewEmail(),
            ["password"] = ApiClient.Password,
            [field] = new[] { "curator" },
        };

        var response = await client.PostAsJsonAsync("/api/v1/accounts", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(field, (await response.ReadAsync<ProblemDto>()).Errors!.Keys);
    }
}
