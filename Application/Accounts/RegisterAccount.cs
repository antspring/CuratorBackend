namespace Application.Accounts;

public sealed record RegisterAccountCommand(
    string Email,
    string Password,
    string FirstName,
    string? LastName,
    string Timezone,
    IReadOnlyCollection<string> Roles);

public sealed record Account(Guid Id, string Email, IReadOnlyList<string> Roles);

public abstract record RegisterAccountResult
{
    public sealed record Registered(Account Account) : RegisterAccountResult;

    public sealed record EmailAlreadyExists : RegisterAccountResult;

    public sealed record PasswordRejected(IReadOnlyList<string> Errors) : RegisterAccountResult;
}
