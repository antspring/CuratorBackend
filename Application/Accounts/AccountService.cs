namespace Application.Accounts;

public sealed class AccountService(IAccountStore accountStore)
{
    public Task<RegisterAccountResult> RegisterAsync(RegisterAccountCommand command, CancellationToken cancellationToken) =>
        accountStore.CreateAsync(command.Email.Trim(), command.Password, cancellationToken);

    public Task<Account?> FindAsync(Guid userId, CancellationToken cancellationToken) =>
        accountStore.FindByIdAsync(userId, cancellationToken);
}
