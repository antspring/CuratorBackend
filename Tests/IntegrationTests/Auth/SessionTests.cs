using System.Net;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Auth;

public class SessionTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Login_ReturnsTokensThatAuthorizeMe()
    {
        var tokens = await client.RegisterAndLoginAsync("mentor");

        var me = await client.GetMeAsync(tokens.AccessToken);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var account = await me.ReadAsync<AccountDto>();
        Assert.Equal(tokens.Account.Id, account.Id);
        Assert.Equal(["mentor"], account.Roles);
        Assert.InRange(tokens.AccessTokenExpiresAt - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task Login_ResponseIsNotCached()
    {
        var email = ApiClient.NewEmail();
        (await client.RegisterAsync(email)).EnsureSuccessStatusCode();

        var response = await client.LoginAsync(email);

        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_ReturnSameError()
    {
        var email = ApiClient.NewEmail();
        (await client.RegisterAsync(email)).EnsureSuccessStatusCode();

        var wrongPassword = await client.LoginAsync(email, "wrong password!!");
        var unknownEmail = await client.LoginAsync(ApiClient.NewEmail());

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal("invalid_credentials", (await wrongPassword.ReadAsync<ProblemDto>()).Code);
        Assert.Equal("invalid_credentials", (await unknownEmail.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Login_AfterFiveFailures_IsLockedOutEvenWithCorrectPassword()
    {
        var email = ApiClient.NewEmail();
        (await client.RegisterAsync(email)).EnsureSuccessStatusCode();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await client.LoginAsync(email, "wrong password!!");
        }

        var response = await client.LoginAsync(email);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthenticated()
    {
        var response = await client.GetMeAsync(null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("unauthenticated", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Me_WithTamperedToken_ReturnsUnauthorized()
    {
        var tokens = await client.RegisterAndLoginAsync();

        var response = await client.GetMeAsync(tokens.AccessToken[..^2] + "xx");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesToken()
    {
        var tokens = await client.RegisterAndLoginAsync();

        var response = await client.RefreshAsync(tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshed = await response.ReadAsync<TokenPairDto>();
        Assert.NotEqual(tokens.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetMeAsync(refreshed.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_ReusedToken_RevokesWholeSession()
    {
        var tokens = await client.RegisterAndLoginAsync();
        var refreshed = await (await client.RefreshAsync(tokens.RefreshToken)).ReadAsync<TokenPairDto>();

        var reuse = await client.RefreshAsync(tokens.RefreshToken);
        var afterReuse = await client.RefreshAsync(refreshed.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal("invalid_refresh_token", (await reuse.ReadAsync<ProblemDto>()).Code);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshTokenAndIsIdempotent()
    {
        var tokens = await client.RegisterAndLoginAsync();

        var logout = await client.LogoutAsync(tokens.RefreshToken);
        var secondLogout = await client.LogoutAsync(tokens.RefreshToken);
        var refresh = await client.RefreshAsync(tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondLogout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Refresh_UnknownToken_ReturnsInvalidRefreshToken()
    {
        var response = await client.RefreshAsync("garbage");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("invalid_refresh_token", (await response.ReadAsync<ProblemDto>()).Code);
    }
}
