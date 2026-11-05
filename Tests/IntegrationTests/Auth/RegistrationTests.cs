using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Auth;

public class RegistrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Register_ReturnsCreatedAccountWithLocation()
    {
        var email = ApiClient.NewEmail();

        var response = await client.RegisterAsync(email, "mentee", "mentor");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/me", response.Headers.Location?.OriginalString);

        var account = await response.ReadAsync<AccountDto>();
        Assert.Equal(email, account.Email);
        Assert.Equal(["mentee", "mentor"], account.Roles);
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

    [Theory]
    [InlineData("curator")]
    [InlineData("administrator")]
    public async Task Register_PrivilegedRole_ReturnsValidationError(string role)
    {
        var response = await client.RegisterAsync(ApiClient.NewEmail(), role);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal("validation_failed", problem.Code);
        Assert.Contains("roles", problem.Errors!.Keys);
    }

    [Fact]
    public async Task Register_ShortPasswordAndMissingName_ReturnsFieldErrors()
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            email = ApiClient.NewEmail(),
            password = "short",
            timezone = "UTC",
            roles = new[] { "mentee" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Contains("password", problem.Errors!.Keys);
        Assert.Contains("firstName", problem.Errors.Keys);
    }

    [Fact]
    public async Task Register_UnknownJsonField_ReturnsValidationError()
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            email = ApiClient.NewEmail(),
            password = ApiClient.Password,
            firstName = "Анна",
            timezone = "UTC",
            roles = new[] { "mentee" },
            isAdmin = true,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("isAdmin", (await response.ReadAsync<ProblemDto>()).Errors!.Keys);
    }
}
