using Application.Accounts;

namespace Application.Sessions;

public sealed class SessionService(IAccountStore accountStore, ITokenService tokenService)
{
    public async Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var account = await accountStore.VerifyCredentialsAsync(command.Email.Trim(), command.Password, cancellationToken);

        if (account is null)
        {
            return new LoginResult.InvalidCredentials();
        }

        var tokens = await tokenService.IssueAsync(account, cancellationToken);

        return new LoginResult.Succeeded(account, tokens);
    }

    public async Task<RefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var consumed = await tokenService.ConsumeRefreshTokenAsync(refreshToken, cancellationToken);

        if (consumed is null)
        {
            return new RefreshResult.InvalidRefreshToken();
        }

        var account = await accountStore.FindByIdAsync(consumed.UserId, cancellationToken);

        if (account is null)
        {
            return new RefreshResult.InvalidRefreshToken();
        }

        var tokens = await tokenService.IssueAsync(account, cancellationToken, consumed.TokenId);

        return new RefreshResult.Succeeded(account, tokens);
    }

    public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken) =>
        tokenService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
}
