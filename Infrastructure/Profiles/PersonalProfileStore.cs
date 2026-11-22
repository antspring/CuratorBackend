using Application.Profiles;
using Domain.Users;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Profiles;

internal sealed record PersonalSnapshot(UserProfile Profile, string? PhotoStorageKey);

internal sealed class PersonalProfileStore(AppDbContext db)
{
    public async Task<PersonalSnapshot> LoadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await db.UserProfiles.AsNoTracking()
            .SingleAsync(existing => existing.UserId == userId, cancellationToken);

        var photoStorageKey = profile.PhotoFileId is { } photoFileId
            ? await db.Files.AsNoTracking()
                .Where(file => file.Id == photoFileId)
                .Select(file => file.StorageKey)
                .SingleAsync(cancellationToken)
            : null;

        return new PersonalSnapshot(profile, photoStorageKey);
    }

    public async Task<SaveProfileOutcome?> StageAsync(
        Guid userId,
        string ownRole,
        string conflictingRole,
        PersonalData personal,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var locked = await db.Database
            .SqlQuery<Guid>($"SELECT id AS \"Value\" FROM users WHERE id = {userId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        if (locked.Count == 0)
        {
            return SaveProfileOutcome.AccountNotFound;
        }

        var roles = await db.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Join(db.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name!)
            .ToListAsync(cancellationToken);

        if (!roles.Contains(ownRole))
        {
            if (roles.Contains(conflictingRole))
            {
                return SaveProfileOutcome.RoleConflict;
            }

            if (roles.Count > 0)
            {
                return SaveProfileOutcome.Forbidden;
            }

            var roleId = await db.Roles
                .Where(role => role.Name == ownRole)
                .Select(role => role.Id)
                .SingleAsync(cancellationToken);

            db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = roleId });
        }

        var profile = await db.UserProfiles.SingleOrDefaultAsync(existing => existing.UserId == userId, cancellationToken);

        if (profile is null)
        {
            db.UserProfiles.Add(new UserProfile(userId, personal, now));
        }
        else
        {
            profile.Update(personal, now);
        }

        return null;
    }
}
