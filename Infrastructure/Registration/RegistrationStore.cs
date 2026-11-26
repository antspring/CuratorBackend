using System.Transactions;
using Application.Accounts;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Registration;

internal sealed class RegistrationStore(
    AppDbContext db,
    UserManager<ApplicationUser> users,
    VerificationCodes codes,
    TimeProvider clock) : IRegistrationStore
{
    private DbSet<PendingRegistration> Pending => db.Set<PendingRegistration>();

    public async Task<RegisterAccountResult> StartAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalized = users.NormalizeEmail(email)!;
        using var transaction = BeginTransaction();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({normalized}, 0))", cancellationToken);
        var row = await Pending.FromSqlInterpolated(
            $"SELECT * FROM pending_registrations WHERE normalized_email = {normalized} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (await users.FindByEmailAsync(email) is not null)
        {
            return new RegisterAccountResult.EmailAlreadyExists();
        }

        var now = clock.GetUtcNow();
        if (row?.ResendAvailableAt > now)
        {
            return new RegisterAccountResult.TooSoon(row.ResendAvailableAt - now);
        }

        var user = new ApplicationUser { Email = email, UserName = email };
        foreach (var validator in users.PasswordValidators)
        {
            var result = await validator.ValidateAsync(users, user, password);
            if (!result.Succeeded)
            {
                return new RegisterAccountResult.PasswordRejected(result.Errors.Select(error => error.Description).ToArray());
            }
        }

        if (row is null)
        {
            row = new PendingRegistration { NormalizedEmail = normalized };
            Pending.Add(row);
        }

        row.Email = email;
        row.PasswordHash = users.PasswordHasher.HashPassword(user, password);
        row.VerificationId = Guid.NewGuid();
        var ticket = row.IssueCode(now, codes);
        await db.SaveChangesAsync(cancellationToken);
        transaction.Complete();
        return new RegisterAccountResult.Pending(ticket);
    }

    public async Task<RegisterAccountResult> ConfirmAsync(Guid verificationId, string code, CancellationToken cancellationToken)
    {
        using var transaction = BeginTransaction();
        var row = await LockAsync(verificationId, cancellationToken);
        if (row is null || row.ExpiresAt <= clock.GetUtcNow() || row.AttemptsLeft == 0)
        {
            return new RegisterAccountResult.Unavailable();
        }

        if (!codes.Matches(row.CodeNonce, code))
        {
            row.AttemptsLeft--;
            await db.SaveChangesAsync(cancellationToken);
            transaction.Complete();
            return row.AttemptsLeft == 0 ? new RegisterAccountResult.Unavailable() : new RegisterAccountResult.InvalidCode();
        }

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = row.Email,
            Email = row.Email,
            EmailConfirmed = true,
            PasswordHash = row.PasswordHash,
        };
        var created = await users.CreateAsync(user);
        if (!created.Succeeded)
        {
            if (created.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return new RegisterAccountResult.EmailAlreadyExists();
            }

            throw new InvalidOperationException(string.Join(", ", created.Errors.Select(error => error.Code)));
        }

        Pending.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
        transaction.Complete();
        return new RegisterAccountResult.Registered(new Account(user.Id, row.Email, []));
    }

    public async Task<RegisterAccountResult> ResendAsync(Guid verificationId, CancellationToken cancellationToken)
    {
        using var transaction = BeginTransaction();
        var row = await LockAsync(verificationId, cancellationToken);
        var now = clock.GetUtcNow();
        if (row is null || row.ExpiresAt + PendingRegistration.RetentionAfterExpiry <= now)
        {
            return new RegisterAccountResult.Unavailable();
        }

        if (row.ResendAvailableAt > now)
        {
            return new RegisterAccountResult.TooSoon(row.ResendAvailableAt - now);
        }

        var ticket = row.IssueCode(now, codes);
        await db.SaveChangesAsync(cancellationToken);
        transaction.Complete();
        return new RegisterAccountResult.Pending(ticket);
    }

    private Task<PendingRegistration?> LockAsync(Guid id, CancellationToken cancellationToken) =>
        Pending.FromSqlInterpolated($"SELECT * FROM pending_registrations WHERE verification_id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private static TransactionScope BeginTransaction() =>
        new(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);
}
