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
}
