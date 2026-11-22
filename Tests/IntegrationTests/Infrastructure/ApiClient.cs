using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace IntegrationTests.Infrastructure;

public sealed record AccountDto(Guid Id, string Email, string[] Roles);

public sealed record TokenPairDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    AccountDto Account);

public sealed record ProblemDto(string Type, string Title, int Status, string Code, string TraceId, Dictionary<string, string[]>? Errors);

public static class ApiClient
{
    public const string Password = "correct horse battery staple";

    public static string NewEmail() => $"user.{Guid.NewGuid():N}@example.com";

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/v1/accounts", new { email, password = Password });

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/v1/session", new { email, password });

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/v1/session/refresh", new { refreshToken });

    public static Task<HttpResponseMessage> LogoutAsync(this HttpClient client, string refreshToken) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/session")
        {
            Content = JsonContent.Create(new { refreshToken }),
        });

    public static Task<HttpResponseMessage> GetMeAsync(this HttpClient client, string? accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return client.SendAsync(request);
    }

    public static async Task<TokenPairDto> RegisterAndLoginAsync(this HttpClient client)
    {
        var email = NewEmail();
        (await client.RegisterAsync(email)).EnsureSuccessStatusCode();

        var login = await client.LoginAsync(email);
        login.EnsureSuccessStatusCode();

        return await login.ReadAsync<TokenPairDto>();
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(JsonSerializerOptions.Web))!;
}
