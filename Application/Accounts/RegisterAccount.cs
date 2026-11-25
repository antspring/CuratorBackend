namespace Application.Accounts;

public sealed record RegisterAccountCommand(string Email, string Password);

public sealed record Account(Guid Id, string Email, IReadOnlyList<string> Roles);

public sealed record VerificationTicket(Guid VerificationId, DateTimeOffset ExpiresAt, DateTimeOffset ResendAvailableAt);

public abstract record RegisterAccountResult
{
    public sealed record Pending(VerificationTicket Ticket) : RegisterAccountResult;
    public sealed record Registered(Account Account) : RegisterAccountResult;
    public sealed record EmailAlreadyExists : RegisterAccountResult;
    public sealed record PasswordRejected(IReadOnlyList<string> Errors) : RegisterAccountResult;
    public sealed record InvalidCode : RegisterAccountResult;
    public sealed record Unavailable : RegisterAccountResult;
    public sealed record TooSoon(TimeSpan RetryAfter) : RegisterAccountResult;
}
