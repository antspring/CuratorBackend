using Domain.Common;
using Domain.Users;

namespace Application.Accounts;

public sealed class AccountService(IAccountStore accountStore, TimeProvider timeProvider)
{
    public Task<RegisterAccountResult> RegisterAsync(RegisterAccountCommand command, CancellationToken cancellationToken)
    {
        var roles = NormalizeRoles(command.Roles);
        var profile = new UserProfile(
            Guid.CreateVersion7(),
            command.FirstName,
            command.LastName,
            command.Timezone,
            timeProvider.GetUtcNow());

        return accountStore.CreateAsync(command.Email.Trim(), command.Password, roles, profile, cancellationToken);
    }

    public Task<Account?> FindAsync(Guid userId, CancellationToken cancellationToken) =>
        accountStore.FindByIdAsync(userId, cancellationToken);

    private static IReadOnlyList<string> NormalizeRoles(IReadOnlyCollection<string> roles)
    {
        var normalized = roles.Select(role => role.Trim().ToLowerInvariant()).ToList();

        if (normalized.Count == 0)
        {
            throw new DomainException(nameof(RegisterAccountCommand.Roles), "Выберите хотя бы одну роль.");
        }

        if (normalized.Distinct().Count() != normalized.Count)
        {
            throw new DomainException(nameof(RegisterAccountCommand.Roles), "Роли не должны повторяться.");
        }

        if (normalized.Any(role => !Roles.SelfAssignable.Contains(role)))
        {
            throw new DomainException(
                nameof(RegisterAccountCommand.Roles),
                $"Допустимые роли: {string.Join(", ", Roles.SelfAssignable)}.");
        }

        return normalized;
    }
}
