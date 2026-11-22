using Application.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Identity;

internal sealed class AccountStore(
    UserManager<ApplicationUser> userManager,
    IPasswordHasher<ApplicationUser> passwordHasher) : IAccountStore
{
    private static readonly string[] DuplicateErrorCodes = ["DuplicateEmail", "DuplicateUserName"];
    private static readonly ApplicationUser TimingUser = new();
    private static string? timingHash;

    public async Task<Account?> VerifyCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            timingHash ??= passwordHasher.HashPassword(TimingUser, Guid.NewGuid().ToString());
            passwordHasher.VerifyHashedPassword(TimingUser, timingHash, password);
            return null;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return null;
        }

        if (user.AccessFailedCount > 0)
        {
            await userManager.ResetAccessFailedCountAsync(user);
        }

        return await ToAccountAsync(user);
    }

    public async Task<Account?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        return user is null ? null : await ToAccountAsync(user);
    }

    private async Task<Account> ToAccountAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        return new Account(user.Id, user.Email!, roles.Order().ToList());
    }

    public async Task<RegisterAccountResult> CreateAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
        };

        try
        {
            var created = await userManager.CreateAsync(user, password);

            if (!created.Succeeded)
            {
                return created.Errors.Any(error => DuplicateErrorCodes.Contains(error.Code))
                    ? new RegisterAccountResult.EmailAlreadyExists()
                    : new RegisterAccountResult.PasswordRejected(created.Errors.Select(error => error.Description).ToList());
            }
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return new RegisterAccountResult.EmailAlreadyExists();
        }

        return new RegisterAccountResult.Registered(new Account(user.Id, email, []));
    }
}
