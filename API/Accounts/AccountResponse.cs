using Application.Accounts;

namespace API.Accounts;

public sealed record AccountResponse(Guid Id, string Email, IReadOnlyList<string> Roles)
{
    public static AccountResponse From(Account account) => new(account.Id, account.Email, account.Roles);
}
