using Application.Accounts;

namespace Application.Sessions;

public sealed record LoginCommand(string Email, string Password);

public sealed record TokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public abstract record LoginResult
{
    public sealed record Succeeded(Account Account, TokenPair Tokens) : LoginResult;

    public sealed record InvalidCredentials : LoginResult;
}
