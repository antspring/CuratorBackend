namespace Application.Accounts;

public interface IAccountStore
{
    Task<Account?> VerifyCredentialsAsync(string email, string password, CancellationToken cancellationToken);

    Task<Account?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);
}
