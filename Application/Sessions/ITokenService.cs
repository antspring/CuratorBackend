using Application.Accounts;

namespace Application.Sessions;

public interface ITokenService
{
    Task<TokenPair> IssueAsync(Account account, CancellationToken cancellationToken);
}
