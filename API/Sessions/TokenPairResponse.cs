using API.Accounts;
using Application.Accounts;
using Application.Sessions;

namespace API.Sessions;

public sealed record TokenPairResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    AccountResponse Account)
{
    public static TokenPairResponse From(Account account, TokenPair tokens) => new(
        tokens.AccessToken,
        tokens.AccessTokenExpiresAt,
        tokens.RefreshToken,
        tokens.RefreshTokenExpiresAt,
        AccountResponse.From(account));
}
