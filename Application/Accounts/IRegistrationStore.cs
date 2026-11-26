namespace Application.Accounts;

public interface IRegistrationStore
{
    Task<RegisterAccountResult> StartAsync(string email, string password, CancellationToken cancellationToken);
    Task<RegisterAccountResult> ConfirmAsync(Guid verificationId, string code, CancellationToken cancellationToken);
    Task<RegisterAccountResult> ResendAsync(Guid verificationId, CancellationToken cancellationToken);
}
