namespace Application.Accounts;

public sealed class AccountService(IAccountStore accountStore, IRegistrationStore registrations)
{
    public Task<RegisterAccountResult> RegisterAsync(RegisterAccountCommand command, CancellationToken cancellationToken) =>
        registrations.StartAsync(command.Email.Trim(), command.Password, cancellationToken);

    public Task<RegisterAccountResult> ConfirmAsync(Guid verificationId, string code, CancellationToken cancellationToken) =>
        registrations.ConfirmAsync(verificationId, code, cancellationToken);

    public Task<RegisterAccountResult> ResendAsync(Guid verificationId, CancellationToken cancellationToken) =>
        registrations.ResendAsync(verificationId, cancellationToken);

    public Task<Account?> FindAsync(Guid userId, CancellationToken cancellationToken) =>
        accountStore.FindByIdAsync(userId, cancellationToken);
}
