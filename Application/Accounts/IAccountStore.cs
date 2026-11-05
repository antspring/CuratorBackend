using Domain.Users;

namespace Application.Accounts;

public interface IAccountStore
{
    Task<RegisterAccountResult> CreateAsync(
        string email,
        string password,
        IReadOnlyList<string> roles,
        UserProfile profile,
        CancellationToken cancellationToken);
}
