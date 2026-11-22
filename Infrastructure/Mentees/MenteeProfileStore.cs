using System.Transactions;
using Application.Mentees;
using Domain.Mentees;
using Domain.Users;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Mentees;

internal sealed class MenteeProfileStore(AppDbContext db) : IMenteeProfileStore
{
    public async Task<MenteeProfileSnapshot?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var mentee = await db.MenteeProfiles.AsNoTracking()
            .Include(profile => profile.Skills)
            .Include(profile => profile.Tasks)
            .AsSplitQuery()
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        if (mentee is null)
        {
            return null;
        }

        var personal = await db.UserProfiles.AsNoTracking()
            .SingleAsync(profile => profile.UserId == userId, cancellationToken);

        var photoStorageKey = personal.PhotoFileId is { } photoFileId
            ? await db.Files.AsNoTracking()
                .Where(file => file.Id == photoFileId)
                .Select(file => file.StorageKey)
                .SingleAsync(cancellationToken)
            : null;

        return new MenteeProfileSnapshot(personal, mentee, photoStorageKey);
    }

    public async Task<SaveMenteeProfileOutcome> SaveAsync(
        Guid userId,
        PersonalData personal,
        MenteeAnswers answers,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = Transaction.Current is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var locked = await db.Database
            .SqlQuery<Guid>($"SELECT id AS \"Value\" FROM users WHERE id = {userId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        if (locked.Count == 0)
        {
            return SaveMenteeProfileOutcome.AccountNotFound;
        }

        var roles = await db.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Join(db.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name!)
            .ToListAsync(cancellationToken);

        if (!roles.Contains(Roles.Mentee))
        {
            if (roles.Contains(Roles.Mentor))
            {
                return SaveMenteeProfileOutcome.RoleConflict;
            }

            if (roles.Count > 0)
            {
                return SaveMenteeProfileOutcome.Forbidden;
            }

            var menteeRoleId = await db.Roles
                .Where(role => role.Name == Roles.Mentee)
                .Select(role => role.Id)
                .SingleAsync(cancellationToken);

            db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = menteeRoleId });
        }

        var profile = await db.UserProfiles.SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        if (profile is null)
        {
            db.UserProfiles.Add(new UserProfile(userId, personal, now));
        }
        else
        {
            profile.Update(personal, now);
        }

        var mentee = await db.MenteeProfiles
            .Include(existing => existing.Skills)
            .Include(existing => existing.Tasks)
            .AsSplitQuery()
            .SingleOrDefaultAsync(existing => existing.UserId == userId, cancellationToken);

        if (mentee is null)
        {
            db.MenteeProfiles.Add(new MenteeProfile(userId, answers));
        }
        else
        {
            mentee.Update(answers);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return SaveMenteeProfileOutcome.Saved;
    }
}
