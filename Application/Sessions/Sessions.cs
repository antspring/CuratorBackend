using Application.Accounts;

namespace Application.Sessions;

public sealed record LoginCommand(string Email, string Password);

public sealed record TokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record ConsumedRefreshToken(Guid TokenId, Guid UserId);

public abstract record LoginResult
{
    public sealed record Succeeded(Account Account, TokenPair Tokens) : LoginResult;

    public sealed record InvalidCredentials : LoginResult;
}

public abstract record RefreshResult
{
    public sealed record Succeeded(Account Account, TokenPair Tokens) : RefreshResult;

    public sealed record InvalidRefreshToken : RefreshResult;
}
