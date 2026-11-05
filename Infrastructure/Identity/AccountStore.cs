using Application.Accounts;
using Domain.Users;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Identity;

internal sealed class AccountStore(AppDbContext dbContext, UserManager<ApplicationUser> userManager) : IAccountStore
{
    private static readonly string[] DuplicateErrorCodes = ["DuplicateEmail", "DuplicateUserName"];

    public async Task<RegisterAccountResult> CreateAsync(
        string email,
        string password,
        IReadOnlyList<string> roles,
        UserProfile profile,
        CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = profile.UserId,
            UserName = email,
            Email = email,
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var created = await userManager.CreateAsync(user, password);

            if (!created.Succeeded)
            {
                return created.Errors.Any(error => DuplicateErrorCodes.Contains(error.Code))
                    ? new RegisterAccountResult.EmailAlreadyExists()
                    : new RegisterAccountResult.PasswordRejected(created.Errors.Select(error => error.Description).ToList());
            }

            var rolesAdded = await userManager.AddToRolesAsync(user, roles);

            if (!rolesAdded.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Не удалось назначить роли: {string.Join("; ", rolesAdded.Errors.Select(error => error.Code))}");
            }

            dbContext.UserProfiles.Add(profile);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return new RegisterAccountResult.EmailAlreadyExists();
        }

        return new RegisterAccountResult.Registered(new Account(user.Id, email, roles));
    }
}
