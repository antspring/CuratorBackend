using Application.Accounts;

namespace Application.Sessions;

public interface ITokenService
{
    Task<TokenPair> IssueAsync(Account account, CancellationToken cancellationToken, Guid? replacesTokenId = null);

    Task<ConsumedRefreshToken?> ConsumeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
}
