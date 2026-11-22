using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Auth;

public class RegistrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Register_ReturnsCreatedAccountWithoutRoles()
    {
        var email = ApiClient.NewEmail();

        var response = await client.RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/me", response.Headers.Location?.OriginalString);

        var account = await response.ReadAsync<AccountDto>();
        Assert.Equal(email, account.Email);
        Assert.Empty(account.Roles);
        Assert.NotEqual(Guid.Empty, account.Id);
    }

    [Fact]
    public async Task Register_SameEmailInOtherCase_ReturnsAccountExists()
    {
        var email = ApiClient.NewEmail();
        (await client.RegisterAsync(email)).EnsureSuccessStatusCode();

        var response = await client.RegisterAsync(email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("account_exists", (await response.ReadAsync<ProblemDto>()).Code);
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
