using Application.Accounts;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

internal sealed class AccountStore(
    UserManager<ApplicationUser> userManager,
    IPasswordHasher<ApplicationUser> passwordHasher) : IAccountStore
{
    private static readonly ApplicationUser TimingUser = new();
    private static string? timingHash;

    public async Task<Account?> VerifyCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null || !user.EmailConfirmed)
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
}
